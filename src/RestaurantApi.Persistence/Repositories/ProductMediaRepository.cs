using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class ProductMediaRepository: IProductMediaRepository
{
    private readonly ApiContext _context;

    public ProductMediaRepository(ApiContext context)
    {
        _context = context;
    }

    public void AddRange(List<ProductMedia> productMedia, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.ProductMedias.AddRange(productMedia);
    }
}