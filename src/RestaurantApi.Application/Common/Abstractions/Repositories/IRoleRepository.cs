using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IRoleRepository
{
    IQueryable<AppRole> GetAllAsQueryable();

    Task<Dictionary<Guid, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken);
}
