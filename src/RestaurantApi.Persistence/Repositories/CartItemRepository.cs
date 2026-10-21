using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class CartItemRepository: ICartItemRepository
{
    private readonly ApiContext _context;

    public CartItemRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<CartItem> GetAllAsQueryable()
    {
        return _context.CartItems.AsQueryable();
    }

    public void CreateCartItem(CartItem cartItem, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.CartItems.Add(cartItem);
    }
}