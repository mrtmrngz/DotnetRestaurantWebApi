using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class OrderRepository: IOrderRepository
{
    private readonly ApiContext _context;

    public OrderRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Order> GetAllAsQueryable()
    {
        return _context.Orders.AsQueryable();
    }

    public void CreateOrder(Order order, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.Orders.Add(order);
    }
}