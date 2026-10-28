using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IOrderRepository
{
    IQueryable<Order> GetAllAsQueryable();
    void CreateOrder(Order order, CancellationToken ctx);
}