using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface ICartItemService
{
    void IncreaseCartItemAsync(CartItem cartItem, int quantity, CancellationToken ctx);
}