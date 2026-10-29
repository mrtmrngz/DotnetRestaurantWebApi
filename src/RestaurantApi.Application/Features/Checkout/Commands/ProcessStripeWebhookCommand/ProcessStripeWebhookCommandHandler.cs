using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;

// Stripe checkout ödeme olaylarını siparişe yansıtır.
//
// Güvenlik notları:
// - İmza doğrulaması (HMAC-SHA256 + zaman toleransı) sağlayıcıya ait IPaymentService içinde yapılır;
//   imzası geçersiz istekler buraya ulaşmadan 400 ile reddedilir.
// - Gövdedeki tutar/adres bilgilerine güvenilmez; yalnızca doğrulanmış olayın tipi, oturum kimliği ve
//   oturum açılırken bizim yazdığımız order_id metadata'sı kullanılır.
// - Güncelleme tek SQL ifadesiyle ve "yalnızca ödeme sonucu bekleyen (Pending) sipariş" koşuluyla yapılır;
//   bu koşul mükerrer ya da gecikmiş olayların sonuçlanmış bir ödemeyi geri almasını engeller.
public class ProcessStripeWebhookCommandHandler: IRequestHandler<ProcessStripeWebhookCommand, BaseResponse>
{
    // Stripe oturum ödeme durumları.
    private const string PaidSessionPaymentStatus = "paid";
    private const string NoPaymentRequiredSessionPaymentStatus = "no_payment_required";

    // Ödeme başarısız/iptal edildiğinde uygulanan geçiş (Order.MarkExpired() ile aynı durumlar).
    private static readonly OrderTransition FailedOrderTransition =
        new(PaymentStatus.Failed, OrderStatus.Canceled);

    private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;

    public ProcessStripeWebhookCommandHandler(ILogger<ProcessStripeWebhookCommandHandler> logger,
        IPaymentService paymentService, IOrderRepository orderRepository)
    {
        _logger = logger;
        _paymentService = paymentService;
        _orderRepository = orderRepository;
    }

    public async Task<BaseResponse> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        // (1) imza doğrulaması yapılır ve sağlayıcı olayı uygulama modeline çevrilir
        StripeWebhookEvent stripeEvent = _paymentService.ParseWebhookEvent(request.Payload, request.Signature);

        _logger.LogInformation("Stripe webhook olayı alındı. EventId:{EventId}, EventType:{EventType}",
            stripeEvent.EventId, stripeEvent.EventType);

        // (2) olayın siparişe yansıması belirlenir; işlenmeyen olaylar yoksayılır ve 200 döner
        //     (Stripe yalnızca 2xx dışı yanıtlarda olayı tekrar gönderir).
        OrderTransition? transition = ResolveTransition(stripeEvent);

        if (transition is null)
        {
            _logger.LogInformation(
                "Webhook olayı sipariş durumunu değiştirmedi. EventId:{EventId}, EventType:{EventType}",
                stripeEvent.EventId, stripeEvent.EventType);

            return new BaseResponse
            {
                Code = Codes.STRIPE_WEBHOOK_IGNORED,
                Message = "Olay işlenmedi, istek yoksayıldı."
            };
        }

        // (3) olayın ait olduğu sipariş bulunur
        Guid orderId = await ResolveOrderIdAsync(stripeEvent, cancellationToken);

        // (4) sipariş atomik olarak güncellenir; zaten sonuçlanmışsa hiçbir şey yazılmaz (idempotent)
        int affectedRowCount = await ApplyTransitionAsync(orderId, transition.Value, cancellationToken);

        if (affectedRowCount == 0)
        {
            _logger.LogInformation(
                "Sipariş daha önce sonuçlanmış, güncelleme yapılmadı. OrderId:{OrderId}, EventId:{EventId}",
                orderId, stripeEvent.EventId);

            return new BaseResponse
            {
                Code = Codes.STRIPE_WEBHOOK_PROCESSED,
                Message = "Olay daha önce işlenmiş, sipariş durumu değiştirilmedi."
            };
        }

        _logger.LogInformation(
            "Sipariş ödeme durumu webhook ile güncellendi. OrderId:{OrderId}, PaymentStatus:{PaymentStatus}, OrderStatus:{OrderStatus}",
            orderId, transition.Value.PaymentStatus, transition.Value.OrderStatus);

        return new BaseResponse
        {
            Code = Codes.STRIPE_WEBHOOK_PROCESSED,
            Message = transition.Value.Succeeded
                ? "Ödeme onaylandı, sipariş ödendi olarak işaretlendi."
                : "Ödeme başarısız olduğu için sipariş iptal edildi."
        };
    }

    #region Helper Methods

    // Olay tipini siparişe uygulanacak geçişe çevirir; null dönerse olay yoksayılır.
    private static OrderTransition? ResolveTransition(StripeWebhookEvent stripeEvent)
    {
        return stripeEvent.EventType switch
        {
            // Senkron ödeme: oturum tamamlandı ancak ödemenin alındığı oturumun payment_status alanından doğrulanır
            // (asenkron yöntemlerde oturum "completed" olsa bile ödeme henüz sonuçlanmamış olabilir).
            StripeWebhookEventTypes.CheckoutSessionCompleted when IsSessionPaid(stripeEvent) =>
                new OrderTransition(PaymentStatus.Paid, null),

            // Asenkron ödeme sonucu ayrı olay ile kesinleşir.
            StripeWebhookEventTypes.CheckoutSessionAsyncPaymentSucceeded =>
                new OrderTransition(PaymentStatus.Paid, null),

            // Ödeme başarısız oldu ya da oturumun süresi doldu: sipariş iptal edilir.
            StripeWebhookEventTypes.CheckoutSessionAsyncPaymentFailed => FailedOrderTransition,
            StripeWebhookEventTypes.CheckoutSessionExpired => FailedOrderTransition,

            _ => null
        };
    }

    private static bool IsSessionPaid(StripeWebhookEvent stripeEvent)
    {
        return string.Equals(stripeEvent.SessionPaymentStatus, PaidSessionPaymentStatus,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(stripeEvent.SessionPaymentStatus, NoPaymentRequiredSessionPaymentStatus,
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> ResolveOrderIdAsync(StripeWebhookEvent stripeEvent, CancellationToken cancellationToken)
    {
        Guid? orderIdFromMetadata = Guid.TryParse(stripeEvent.OrderId, out Guid parsedOrderId)
            ? parsedOrderId
            : null;

        string? sessionId = string.IsNullOrWhiteSpace(stripeEvent.CheckoutSessionId)
            ? null
            : stripeEvent.CheckoutSessionId;

        // sipariş önce oturum açılırken metadata'ya yazdığımız order_id ile, yoksa sipariş kaydındaki
        // checkout session id ile bulunur
        var order = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => (orderIdFromMetadata.HasValue && x.Id == orderIdFromMetadata.Value)
                        || (sessionId != null && x.CheckoutSessionId == sessionId))
            .Select(x => new { x.Id, x.CheckoutSessionId })
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            // Sessizce yutulmaz: Stripe 2xx dışı yanıtlarda olayı tekrar gönderir, ödeme kaydı kaybolmaz.
            _logger.LogError(
                "Webhook olayına ait sipariş bulunamadı. EventId:{EventId}, EventType:{EventType}, SessionId:{SessionId}, OrderId:{OrderId}",
                stripeEvent.EventId, stripeEvent.EventType, sessionId, stripeEvent.OrderId);

            throw new NotFoundException("Webhook olayına ait sipariş bulunamadı.");
        }

        // olay, siparişin kayıtlı ödeme oturumuna ait olmalıdır (olayın başka bir siparişe yazılmasını engeller)
        if (sessionId != null && !string.IsNullOrEmpty(order.CheckoutSessionId)
                              && !string.Equals(order.CheckoutSessionId, sessionId, StringComparison.Ordinal))
        {
            _logger.LogError(
                "Webhook olayının oturumu siparişte kayıtlı oturumla eşleşmiyor. EventId:{EventId}, OrderId:{OrderId}, OrderSessionId:{OrderSessionId}, EventSessionId:{EventSessionId}",
                stripeEvent.EventId, order.Id, order.CheckoutSessionId, sessionId);

            throw new ConflictException("Webhook olayı siparişin ödeme oturumuyla eşleşmiyor.");
        }

        return order.Id;
    }

    // Ödeme sonucunu tek SQL ifadesiyle yazar; sonuçlanmış siparişlerde hiçbir satır etkilenmez.
    private async Task<int> ApplyTransitionAsync(Guid orderId, OrderTransition transition,
        CancellationToken cancellationToken)
    {
        var query = _orderRepository.GetAllAsQueryable()
            .Where(x => x.Id == orderId && x.PaymentStatus == PaymentStatus.Pending);

        return transition.OrderStatus.HasValue
            ? await query.ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.PaymentStatus, transition.PaymentStatus)
                    .SetProperty(x => x.OrderStatus, transition.OrderStatus.Value),
                cancellationToken)
            : await query.ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.PaymentStatus, transition.PaymentStatus),
                cancellationToken);
    }

    #endregion

    // Olayın siparişe yansıması: yalnızca ödeme durumu ve (gerekiyorsa) sipariş durumu değişir.
    private readonly record struct OrderTransition(PaymentStatus PaymentStatus, OrderStatus? OrderStatus)
    {
        public bool Succeeded => PaymentStatus == PaymentStatus.Paid;
    }
}
