using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IRestaurantTableRepository
{
    IQueryable<RestaurantTable> GetAllAsQueryable();
    void CreateRestaurantTable(RestaurantTable table, CancellationToken ctx);
    Task<RestaurantTable?> GetByIdForUpdateAsync(Guid id, CancellationToken ctx);
    Task<bool> IsTableNumberTakenAsync(int number, Guid excludedTableId, CancellationToken ctx);
}
