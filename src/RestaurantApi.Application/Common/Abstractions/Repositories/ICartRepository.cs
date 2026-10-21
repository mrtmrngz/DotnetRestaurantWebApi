using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface ICartRepository
{
    IQueryable<Cart> GetAllAsQueryable();
    void CreateCart(Cart cart, CancellationToken ctx);
}