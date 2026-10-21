using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface ICartItemRepository
{
    IQueryable<CartItem> GetAllAsQueryable();
    void CreateCartItem(CartItem cartItem, CancellationToken ctx);
}