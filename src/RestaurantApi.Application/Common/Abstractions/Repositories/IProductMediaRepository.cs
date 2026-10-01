using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IProductMediaRepository
{
    void AddRange(List<ProductMedia> productMedia, CancellationToken ctx);
    IQueryable<ProductMedia> GetAllAsQueryable();
    void UpdateRange(List<ProductMedia> productMedias);
}