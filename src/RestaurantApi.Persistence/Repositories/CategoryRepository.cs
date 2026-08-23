using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class CategoryRepository: ICategoryRepository
{
    private readonly ApiContext _context;

    public CategoryRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Category> GetAllAsQueryable()
    {
        return _context.Categories.AsQueryable();
    }

    public void CreateCategory(Category category, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.Categories.Add(category);
    }
}