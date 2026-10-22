using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface ICartService
{
    Task InvalidateCartCache(CartRequestTypes request, Guid? userId, Guid? restaurantTableId);
    Task ValidateTargetEntityAsync(CartRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx);
    Task<Guid> FindCartByRequest(CartRequestTypes type, Guid? userId, Guid? tableId, CancellationToken ctx);
}