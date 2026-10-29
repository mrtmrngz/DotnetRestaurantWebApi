using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.OrderRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;

// Kullanıcı siparişinin durumunu günceller.
// Siparişi getirme ve yokluk kontrolü ortak servistedir (IOrderService); iş kuralları OrderRules
// üzerinden handler içinde uygulanır. Güncelleme tek SQL ifadesiyle yazılır.
public class OrderStatusUpdateCommandHandler: IRequestHandler<OrderStatusUpdateCommand, BaseResponse>
{
    private readonly ILogger<OrderStatusUpdateCommandHandler> _logger;
    private readonly IOrderService _orderService;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderRules _orderRules;

    public OrderStatusUpdateCommandHandler(ILogger<OrderStatusUpdateCommandHandler> logger,
        IOrderService orderService, IOrderRepository orderRepository, OrderRules orderRules)
    {
        _logger = logger;
        _orderService = orderService;
        _orderRepository = orderRepository;
        _orderRules = orderRules;
    }

    public async Task<BaseResponse> Handle(OrderStatusUpdateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sipariş durum güncelleme işlemi başlıyor. OrderId:{OrderId}, UserId:{UserId}, NewStatus:{NewStatus}",
            request.OrderId, request.UserId, request.NewStatus);

        // (1) Sipariş doğrulaması
        await ValidateOrder(request.UserId, request.OrderId, request.NewStatus, cancellationToken);

        // (2) tek SQL ifadesiyle güncellenir; ödeme koşulu, bu sırada değişen bir siparişin üzerine yazılmasını engeller
        int affectedRowCount = await _orderRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.OrderId && x.PaymentStatus == PaymentStatus.Paid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.OrderStatus, request.NewStatus), cancellationToken);

        // (3) sipariş bu sırada değiştiyse (ör. ödeme iptal edildi) hiçbir satır etkilenmez -> 409
        _orderRules.ShouldOrderUpdateBeApplied(affectedRowCount, request.OrderId);

        _logger.LogInformation(
            "Sipariş durumu güncellendi. OrderId:{OrderId}, NewStatus:{NewStatus}, AffectedRowCount:{AffectedRowCount}",
            request.OrderId, request.NewStatus, affectedRowCount);

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = $"Sipariş durumu {request.NewStatus} olarak başarıyla güncellendi."
        };
    }

    #region Helper Method

    private async Task ValidateOrder(Guid userId, Guid orderId, OrderStatus newStatus, CancellationToken ctx)
    {
        // (1) sipariş bulunur; yoksa 404 (ortak adım: IOrderService.FindAndValidateOrderAsync)
        _logger.LogInformation("Sipariş aranıyor. OrderId:{OI}, UserId:{UI}", orderId, userId);
        Domain.Entities.Order order = await _orderService.FindAndValidateOrderAsync(orderId, ctx);

        // (2) sipariş başka bir kullanıcıya aitse 403 (masa siparişlerinde UserId boş olduğu için onlar da reddedilir)
        _logger.LogInformation("Sipariş sahibi doğrulanıyor.");
        _orderRules.ShouldOrderBelongToUser(order, userId);

        // (3) gönderilen durum mevcut durumla aynıysa 409
        _logger.LogInformation("Sipariş durumu ile yeni sipariş durumu arasındaki fark doğrulanıyor.");
        _orderRules.ShouldOrderStatusBeDifferent(order.OrderStatus, newStatus, orderId);

        // (4) ödemesi tamamlanmayan siparişin durumu güncellenemez -> 422
        _logger.LogInformation("Sipariş ödeme bilgisi doğrulanıyor.");
        _orderRules.ShouldOrderBePaid(order.PaymentStatus, orderId);
    }

    #endregion
}
