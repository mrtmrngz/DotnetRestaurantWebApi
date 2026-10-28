using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.AddressRules;
using RestaurantApi.Application.Features.Rules.CartRules;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;

public class CreateOnlineCheckoutCommandHandler : IRequestHandler<CreateOnlineCheckoutCommand, CheckoutResponse>
{
    private readonly ILogger<CreateOnlineCheckoutCommandHandler> _logger;
    private readonly ICartService _cartService;
    private readonly IOrderRepository _orderRepository;
    private readonly CartRules _cartRules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAddressRepository _addressRepository;
    private readonly AddressRules _addressRules;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IPaymentService _paymentService;
    private readonly TimeProvider _timeProvider;

    public CreateOnlineCheckoutCommandHandler(ICartService cartService,
        ILogger<CreateOnlineCheckoutCommandHandler> logger, IOrderRepository orderRepository, CartRules cartRules,
        IUnitOfWork unitOfWork, IAddressRepository addressRepository, AddressRules addressRules,
        ICartItemRepository cartItemRepository, IPaymentService paymentService, TimeProvider timeProvider)
    {
        _cartService = cartService;
        _logger = logger;
        _orderRepository = orderRepository;
        _cartRules = cartRules;
        _unitOfWork = unitOfWork;
        _addressRepository = addressRepository;
        _addressRules = addressRules;
        _cartItemRepository = cartItemRepository;
        _paymentService = paymentService;
        _timeProvider = timeProvider;
    }

    public async Task<CheckoutResponse> Handle(CreateOnlineCheckoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ödeme işlemi başlıyor...");

        await ValidateExistIdempotencyKey(request.IdempotencyKey, request.UserId, request.RestaurantTableId,
            cancellationToken);

        // validate user or restaurant id
        await _cartService.ValidateTargetEntityForCheckoutAsync(request.CheckoutRequestTypes, request.UserId,
            request.RestaurantTableId, cancellationToken);

        // validate cart
        Domain.Entities.Cart cart = await ValidateCart(request.CheckoutRequestTypes, request.UserId, request.RestaurantTableId, cancellationToken);

        // create order (ürünün aktif indirimi varsa indirimli birim fiyat kullanılır)
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Order order = Order.CreateFromCart(cart!, request.UserId, request.RestaurantTableId, request.IdempotencyKey,
            nowUtc, OrderStatus.Pending);

        if (request.CheckoutRequestTypes == CheckoutRequestTypes.CurrentUser)
        {
            await FindUserAddressIfRequestCurrentUser(request.UserId!.Value, order, cancellationToken);
        }

        // stripe ödeme oturumu (idempotency key sağlayıcıya iletilir)
        PaymentSessionResult paymentSession = await _paymentService.CreateCheckoutSessionAsync(order,
            request.CheckoutRequestTypes, cancellationToken);

        // oturum id'si ve güvenli ödeme url'i siparişe yazılır
        order.AttachCheckoutSession(paymentSession.SessionId, paymentSession.SessionUrl, paymentSession.ExpiresAt);

        _orderRepository.CreateOrder(order, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sipariş ve ödeme oturumu kaydedildi. OrderId:{OrderId}, SessionId:{SessionId}",
            order.Id, paymentSession.SessionId);

        // ödeme oturumu açıldıktan sonra sepet kalemleri temizlenir (sepet kaydı silinmez)
        await ClearCartItemsAsync(cart!, request, cancellationToken);

        return new CheckoutResponse
        {
            SessionUrl = paymentSession.SessionUrl,
            Code = Codes.CHECKOUT_SESSION_CREATED
        };
    }

    #region HelperMethods

    private async Task ValidateExistIdempotencyKey(string key, Guid? userId, Guid? restaurantTableId,
        CancellationToken ctx)
    {
        var exist = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .AnyAsync(
                x => x.IdempotencyKey == key &&
                     (
                         (userId.HasValue && x.UserId == userId) ||
                         (restaurantTableId.HasValue &&
                          x.RestaurantTableId == restaurantTableId)
                     ),
                ctx);

        if (exist)
        {
            throw new ConflictException(
                "Bu ödeme isteği daha önce başlatılmış. Lütfen ödeme işleminin bitmesini bekleyin.");
        }
    }

    private async Task<Domain.Entities.Cart> ValidateCart(CheckoutRequestTypes checkoutRequestTypes, Guid? userId, Guid? restaurantTableId,
        CancellationToken ctx)
    {
        Domain.Entities.Cart? cart = await _cartService.FindCartWithoutValidateForCheckout(checkoutRequestTypes,
            userId, restaurantTableId, ctx);

        _cartRules.ShouldCartExist(cart);

        // boş sepetle ödeme başlatılamaz (422), domain korumasına düşmeden engellenir
        _cartRules.ShouldCartHaveItems(cart!.CartItems.Any());

        _logger.LogInformation("Sepette silinen ürünler kontrol ediliyor. CartId:{CI}", cart.Id);

        // Product için global query filter tanımlı olduğundan silinmiş ürünlerin Product navigasyonu null gelir,
        // bu yüzden hem null hem IsDeleted kontrol edilir.
        if (cart.CartItems.Any(i => i.Product is null || i.Product.IsDeleted))
        {
            _logger.LogInformation("Sepette silinen ürün tespit edildi.");
            throw new ConflictException("Sepette artık satışta olmayan ürünler var.");
        }

        _logger.LogInformation("Sepette silinen herhangi bir ürün bulunamadı.");
        return cart;
    }

    private async Task ClearCartItemsAsync(Domain.Entities.Cart cart, CreateOnlineCheckoutCommand request,
        CancellationToken ctx)
    {
        var removedItemCount = await _cartItemRepository.GetAllAsQueryable()
            .Where(x => x.CartId == cart.Id)
            .ExecuteDeleteAsync(ctx);

        _logger.LogInformation(
            "Sipariş sonrası sepet kalemleri temizlendi. CartId:{CartId}, RemovedItemCount:{RemovedItemCount}",
            cart.Id, removedItemCount);

        CartRequestTypes cartRequestType = request.CheckoutRequestTypes == CheckoutRequestTypes.CurrentUser
            ? CartRequestTypes.CurrentUser
            : CartRequestTypes.PanelUser;

        await _cartService.InvalidateCartCache(cartRequestType, request.UserId, request.RestaurantTableId);
    }

    private async Task FindUserAddressIfRequestCurrentUser(Guid userId, Order order, CancellationToken ctx)
    {
        _logger.LogInformation("Kullanıcının adresi aranıyor...");
        var address = await _addressRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault, ctx);

        _addressRules.ShouldAddressExist(address, userId);

        order.HandleSetAddress(address!);
    }

    #endregion
}