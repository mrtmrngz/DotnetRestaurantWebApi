using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ApiContext _context;

    public RoleRepository(RoleManager<AppRole> roleManager, ApiContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    public IQueryable<AppRole> GetAllRolesAsQueryable()
    {
        return _roleManager.Roles;
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