using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CartRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateTableCashCheckoutCommand;

public class CreateTableCashCheckoutCommandHandler: IRequestHandler<CreateTableCashCheckoutCommand, BaseResponse>
{
    private readonly ILogger<CreateTableCashCheckoutCommandHandler> _logger;
    private readonly ICartService _cartService;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly CartRules _cartRules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreateTableCashCheckoutCommandHandler(ILogger<CreateTableCashCheckoutCommandHandler> logger,
        ICartService cartService, ICartItemRepository cartItemRepository, IOrderRepository orderRepository,
        CartRules cartRules, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _logger = logger;
        _cartService = cartService;
        _cartItemRepository = cartItemRepository;
        _orderRepository = orderRepository;
        _cartRules = cartRules;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<BaseResponse> Handle(CreateTableCashCheckoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa için nakit ödeme (sipariş) işlemi başlıyor. TableId:{TableId}",
            request.RestaurantTableId);

        // (1) idempotency kontrolü
        await ValidateIdempotencyKeyAsync(request.IdempotencyKey, request.RestaurantTableId, cancellationToken);

        // (2) masa gerçekten var mı ve aktif mi (pasif masa "bulunamadı" olarak döner)
        await _cartService.ValidateTargetEntityForCheckoutAsync(CheckoutRequestTypes.PanelUser, null,
            request.RestaurantTableId, cancellationToken);

        // (3) masanın sepeti doğrulanır: sepet var mı, içinde ürün var mı, satışta olmayan ürün var mı
        Domain.Entities.Cart cart = await ValidateCartAsync(request.RestaurantTableId, cancellationToken);

        // (4) sipariş oluşturulur (nakit ödemede sipariş doğrudan onaylanır)
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Order order = Order.CreateFromCart(cart, null, request.RestaurantTableId, request.IdempotencyKey, nowUtc,
            OrderStatus.Confirmed);

        // nakit ödeme masada yerinde alındığı için sipariş ödeme açısından da tamamlanmış kabul edilir
        order.MarkPaid();

        _orderRepository.CreateOrder(order, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Masa siparişi kaydedildi. OrderId:{OrderId}, TableId:{TableId}, OrderStatus:{OrderStatus}, PaymentStatus:{PaymentStatus}, Total:{Total}",
            order.Id, order.RestaurantTableId, order.OrderStatus, order.PaymentStatus, order.Total);

        // (5) sepet boşaltılır (sepet kaydı korunur, cache geçersiz kılınır)
        await ClearCartItemsAsync(cart, cancellationToken);

        _logger.LogInformation("Masa için nakit ödeme işlemi tamamlandı. OrderId:{OrderId}, TableId:{TableId}",
            order.Id, request.RestaurantTableId);

        return new BaseResponse
        {
            Code = Codes.CHECKOUT_SUCCESS,
            Message = "Masa siparişi oluşturuldu, ödeme nakit olarak alınacaktır."
        };
    }

    #region Helper Methods

    private async Task ValidateIdempotencyKeyAsync(string idempotencyKey, Guid tableId, CancellationToken ctx)
    {
        var alreadyStarted = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey && x.RestaurantTableId == tableId, ctx);

        if (!alreadyStarted)
            return;

        _logger.LogWarning("Aynı idempotency key ile masa siparişi daha önce başlatılmış. TableId:{TableId}", tableId);
        throw new ConflictException(
            "Bu ödeme isteği daha önce başlatılmış. Lütfen ödeme işleminin bitmesini bekleyin.");
    }

    private async Task<Domain.Entities.Cart> ValidateCartAsync(Guid tableId, CancellationToken ctx)
    {
        Domain.Entities.Cart? cart = await _cartService.FindCartWithoutValidateForCheckout(
            CheckoutRequestTypes.PanelUser, null, tableId, ctx);

        _cartRules.ShouldCartExist(cart);

        // sepet boşsa sipariş oluşturulamaz (422)
        _cartRules.ShouldCartHaveItems(cart!.CartItems.Any());

        // Product için global query filter tanımlı olduğundan silinmiş ürünlerin Product navigasyonu null gelir,
        // bu yüzden hem null hem IsDeleted kontrol edilir.
        if (cart.CartItems.Any(i => i.Product is null || i.Product.IsDeleted))
        {
            _logger.LogWarning("Sepette satışta olmayan ürün tespit edildi. CartId:{CartId}", cart.Id);
            throw new ConflictException("Sepette artık satışta olmayan ürünler var.");
        }

        return cart;
    }

    private async Task ClearCartItemsAsync(Domain.Entities.Cart cart, CancellationToken ctx)
    {
        var removedItemCount = await _cartItemRepository.GetAllAsQueryable()
            .Where(x => x.CartId == cart.Id)
            .ExecuteDeleteAsync(ctx);

        _logger.LogInformation(
            "Nakit ödeme sonrası masa sepeti kalemleri temizlendi. CartId:{CartId}, RemovedItemCount:{RemovedItemCount}",
            cart.Id, removedItemCount);

        await _cartService.InvalidateCartCache(CartRequestTypes.PanelUser, null, cart.RestaurantTableId);
    }

    #endregion
}
