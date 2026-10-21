using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class CartRepository: ICartRepository
{
    private readonly ApiContext _context;

    public CartRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Cart> GetAllAsQueryable()
    {
        return _context.Carts.AsQueryable();
    }

    public void CreateCart(Cart cart, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.Carts.Add(cart);
    }
}