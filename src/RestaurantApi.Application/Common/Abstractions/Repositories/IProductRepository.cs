using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IProductRepository
{
    IQueryable<Product> GetAllAsQueryable();
    void Add(Product product, CancellationToken ctx);
}