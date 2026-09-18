using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class ProductRepository: IProductRepository
{
    private readonly ApiContext _context;

    public ProductRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Product> GetAllAsQueryable()
    {
        return _context.Products.AsQueryable();
    }
}