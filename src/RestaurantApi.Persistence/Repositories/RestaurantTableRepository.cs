using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class RestaurantTableRepository: IRestaurantTableRepository
{
    private readonly ApiContext _context;

    public RestaurantTableRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<RestaurantTable> GetAllAsQueryable()
    {
        return _context.RestaurantTables.AsQueryable();
    }

    public void CreateRestaurantTable(RestaurantTable table, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();
        _context.RestaurantTables.Add(table);
    }
}
