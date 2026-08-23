using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface ICategoryRepository
{
    IQueryable<Category> GetAllAsQueryable();
    void CreateCategory(Category category, CancellationToken ctx);
}