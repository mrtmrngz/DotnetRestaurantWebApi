using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface ICartService
{
    Task InvalidateCartCache(CartRequestTypes request, Guid? userId, Guid? restaurantTableId);
    Task ValidateTargetEntityAsync(CartRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx);
    Task ValidateTargetEntityForCheckoutAsync(CheckoutRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx);
    Task<Cart?> FindCartWithoutValidate(CartRequestTypes type, Guid? userId, Guid? tableId,
        CancellationToken ctx);
    Task<Guid> FindCartByRequest(CartRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx);
    Task<Cart?> FindCartWithoutValidateForCheckout(CheckoutRequestTypes type, Guid? userId, Guid? tableId,
        CancellationToken ctx);
}