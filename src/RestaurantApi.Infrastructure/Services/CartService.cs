using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CartRules;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Features.Rules.UserRules;

namespace RestaurantApi.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CartService> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly ICartRepository _cartRepository;
    private readonly CartRules _cartRules;

    public CartService(ICacheService cacheService, ILogger<CartService> logger,
        IRestaurantTableRepository restaurantTableRepository, IUserRepository userRepository, UserRules userRules,
        RestaurantTableRules restaurantTableRules, ICartRepository cartRepository, CartRules cartRules)
    {
        _cacheService = cacheService;
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _userRepository = userRepository;
        _userRules = userRules;
        _restaurantTableRules = restaurantTableRules;
        _cartRepository = cartRepository;
        _cartRules = cartRules;
    }

    public async Task InvalidateCartCache(
        CartRequestTypes request, Guid? userId, Guid? restaurantTableId)
    {
        string cacheKey = request switch
        {
            CartRequestTypes.CurrentUser =>
                CacheKeys.UserCart(userId!.Value),

            CartRequestTypes.PanelUser =>
                CacheKeys.PanelCart(restaurantTableId!.Value),

            _ => throw new BadRequestException(
                "Geçersiz işlem tipi.")
        };

        _logger.LogInformation(
            "Sepet cache temizliği yapılıyor. CacheKey:{CacheKey}",
            cacheKey);

        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "Sepet cache temizliği tamamlandı. CacheKey:{CacheKey}",
            cacheKey);
    }

    public async Task ValidateTargetEntityAsync(CartRequestTypes type, Guid? userId, Guid? tableId,
        CancellationToken ctx)
    {
        switch (type)
        {
            case CartRequestTypes.CurrentUser:
                _logger.LogInformation("Kullanıcı aranıyor.UserId:{UI}", userId);
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Kullanıcı id request içersinde değil.");
                    throw new BadRequestException("Bu işlemi şuanda gerçekleştiremiyoruz.");
                }

                bool userExist = await _userRepository.AnyUserExistAsync(userId.Value, ctx);

                await _userRules.ShouldUserExistBool404(userExist);
                _logger.LogInformation("Kullanıcı bulundu. UserId:{UI}", userId);
                break;
            case CartRequestTypes.PanelUser:
                _logger.LogInformation("Masa aranıyor. TableId{TI}", tableId);
                if (!tableId.HasValue)
                {
                    _logger.LogWarning("Masa id request içerisinde değil.");
                    throw new BadRequestException("Bu işlemi şuanda gerçekleştiremiyoruz.");
                }

                var restaurantTable = await _restaurantTableRepository.GetAllAsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == tableId.Value && x.IsActive, ctx);

                _restaurantTableRules.ShouldTableExist(restaurantTable, tableId.Value);
                _logger.LogInformation("Masa bulundu. TableId{TI}", tableId);
                break;
            default:
                throw new BadRequestException("Geçersiz sepet istek tipi.");
        }
    }

    public async Task<Guid> FindCartByRequest(CartRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx)
    {
        _logger.LogInformation("Sepet aranıyor...");
        var cart = await (type switch
        {
            CartRequestTypes.CurrentUser =>
                _cartRepository.GetAllAsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == userId, ctx),

            CartRequestTypes.PanelUser =>
                _cartRepository.GetAllAsQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.RestaurantTableId == tableId, ctx),

            _ => throw new BadRequestException("Geçersiz işlem tipi.")
        });

        _cartRules.ShouldCartExist(cart);
        
        _logger.LogInformation("Sepet bulundu. CartId:{CI}", cart!.Id);

        return cart!.Id;
    }
}