using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class DiscountRepository: IDiscountRepository
{
    private readonly ApiContext _context;

    public DiscountRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Discount> GetAllAsQueryable()
    {
        return _context.Discounts.AsQueryable();
    }

    public void AddDiscount(Discount discount, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.Discounts.Add(discount);
    }
}
