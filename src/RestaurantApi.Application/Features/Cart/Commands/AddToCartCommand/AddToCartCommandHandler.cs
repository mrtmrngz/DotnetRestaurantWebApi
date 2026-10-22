using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CartItemRules;
using RestaurantApi.Application.Features.Rules.ProductRules;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, BaseResponse>
{
    private readonly ILogger<AddToCartCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly ProductRules _productRules;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly ICartRepository _cartRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly ICartItemService _cartItemService;
    private readonly CartItemRules _cartItemRules;
    private readonly ICartService _cartService;

    public AddToCartCommandHandler(ILogger<AddToCartCommandHandler> logger, ICacheService cacheService,
        IUnitOfWork unitOfWork, IProductRepository productRepository, ProductRules productRules,
        IUserRepository userRepository, UserRules userRules, ICartRepository cartRepository,
        ICartItemRepository cartItemRepository,
        IRestaurantTableRepository restaurantTableRepository, RestaurantTableRules restaurantTableRules,
        ICartItemService cartItemService, CartItemRules cartItemRules, ICartService cartService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _productRules = productRules;
        _userRepository = userRepository;
        _userRules = userRules;
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _restaurantTableRepository = restaurantTableRepository;
        _restaurantTableRules = restaurantTableRules;
        _cartItemService = cartItemService;
        _cartItemRules = cartItemRules;
        _cartService = cartService;
    }

    public async Task<BaseResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sepete ürün ekleme işlemi başlıyor...ProductId:{PrdId}, Quantity:{Q}, RequestType:{RT}",
            request.ProductId, request.Quantity, request.RequestType);

        await ValidateProduct(request.ProductId, cancellationToken);

        await Orchestrator(request, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cartService.InvalidateCartCache(request.RequestType, request.UserId, request.RestaurantTableId);
        
        _logger.LogInformation("Sepete oluşturma/ekleme işlemleri tamamlandı.");

        return new BaseResponse
        {
            Message = "Ürün sepete başarılı bir şekilde eklendi.",
            Code = Codes.ADD_TO_CART_SUCCESS
        };
    }

    #region Helper Methods

    private async Task ValidateProduct(Guid productId, CancellationToken ctx)
    {
        _logger.LogInformation("Ürün aranıyor... ProductId:{PrdId}", productId);

        var product = await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId, ctx);

        _productRules.ValidateExistProductEntity(product, productId);

        _productRules.ShouldProductActive(product!.IsDeleted, productId);
    }

    private async Task Orchestrator(AddToCartCommand command, CancellationToken ctx)
    {
        switch (command.RequestType)
        {
            case CartRequestTypes.CurrentUser:
                await AddToCartUser(command, ctx);
                break;
            case CartRequestTypes.PanelUser:
                await AddToCartRestaurantTable(command, ctx);
                break;
            default:
                throw new BadRequestException("Geçersiz işlem tipi.");
        }
    }

    private async Task AddToCartUser(AddToCartCommand command, CancellationToken ctx)
    {
        _logger.LogInformation("Kullanıcı aranıyor.");

        await _userRules.ShouldUserExistBool404(await _userRepository.AnyUserExistAsync(command.UserId!.Value, ctx));

        _logger.LogInformation("Kullanıcı başarılı bir şekilde bulundu.");

        _logger.LogInformation("Kullanıcı sepeti aranıyor...");

        var userCart = await _cartRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == command.UserId!.Value, ctx);

        if (userCart is null)
        {
            _logger.LogInformation("Kullanıcı sepeti bulunamadı, yeni sepet oluşturuluyor.");

            userCart = new Domain.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId
            };

            _cartRepository.CreateCart(userCart, ctx);

            _logger.LogInformation("Kullanıcı sepeti oluşturuldu. CartId:{CI}", userCart.Id);
        }

        _logger.LogInformation("Ürün sepete ekleniyor.");

        await HandleCartItem(userCart.Id, command.ProductId, command.Quantity, ctx);

        _logger.LogInformation("Ürün sepete eklendi.");
    }

    private async Task AddToCartRestaurantTable(AddToCartCommand command, CancellationToken ctx)
    {
        _logger.LogInformation("Masa araniyor..");

        var existTable = await _restaurantTableRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.RestaurantTableId && x.IsActive, ctx);

        _restaurantTableRules.ShouldTableExist(existTable, command.RestaurantTableId!.Value);

        _logger.LogInformation("Masa bulundu: TableId{MI}", command.RestaurantTableId);
        
        _logger.LogInformation("Masa sepeti aranıyor...");

        var tableCart = await _cartRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RestaurantTableId == command.RestaurantTableId, ctx);

        if (tableCart == null)
        {
            _logger.LogInformation("Masa sepeti bulunamadı, yeni sepet oluşturuluyor.");

            tableCart = new Domain.Entities.Cart
            {
                Id = Guid.NewGuid(),
                RestaurantTableId = command.RestaurantTableId
            };

            _cartRepository.CreateCart(tableCart, ctx);

            _logger.LogInformation("Masa sepeti oluşturuldu. SepetId:{CI}", tableCart.Id);
        }

        await HandleCartItem(tableCart.Id, command.ProductId, command.Quantity, ctx);
        _logger.LogInformation("Ürün sepete eklendi.");
    }

    private async Task HandleCartItem(Guid cartId, Guid productId, int quantity, CancellationToken ctx)
    {
        var existCartItem = await _cartItemRepository.GetAllAsQueryable()
            .FirstOrDefaultAsync(x => x.CartId == cartId && x.ProductId == productId, ctx);

        if (existCartItem == null)
        {
            _cartItemRules.CheckMaximumCartValue(quantity);
            
            var newCartItem = new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                CartId = cartId
            };
            
            _cartItemRepository.CreateCartItem(newCartItem, ctx);

            return;
        }

        _cartItemService.IncreaseCartItemAsync(existCartItem, quantity, ctx);
    }

    #endregion
}