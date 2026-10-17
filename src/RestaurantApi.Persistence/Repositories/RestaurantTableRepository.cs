using Microsoft.EntityFrameworkCore;
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

    public async Task<RestaurantTable?> GetByIdForUpdateAsync(Guid id, CancellationToken ctx)
    {
        ctx.ThrowIfCancellationRequested();

        // PostgreSQL satır kilidi: transaction bitene kadar ilgili masa satırı kilitli kalır.
        // EF Core bu ham sorguyu sarmalamadan çalıştırabilmesi için ToListAsync kullanılıyor.

        var tables = await _context.RestaurantTables
            .FromSqlRaw("SELECT * FROM restaurant_tables WHERE \"Id\" = {0} FOR UPDATE", id)
            .ToListAsync(ctx);

        return tables.FirstOrDefault();
    }

    public Task<bool> IsTableNumberTakenAsync(int number, Guid excludedTableId, CancellationToken ctx)
    {
        return _context.RestaurantTables
            .AnyAsync(t => t.Number == number && t.Id != excludedTableId, ctx);
    }
}
