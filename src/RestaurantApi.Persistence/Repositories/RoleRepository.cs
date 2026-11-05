using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Identity;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class RoleRepository: IRoleRepository
{

    private readonly ApiContext _context;

    public RoleRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<AppRole> GetAllAsQueryable()
    {
        return _context.Roles.AsQueryable();
    }

    public async Task<Dictionary<Guid, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .GroupBy(userRole => userRole.RoleId)
            .Select(group => new { RoleId = group.Key, UserCount = group.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.UserCount, cancellationToken);
    }
}
