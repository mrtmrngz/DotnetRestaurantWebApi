using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class PermissionRepository: IPermissionRepository
{

    private readonly ApiContext _context;

    public PermissionRepository(ApiContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetPermissionsByRoleNameAsync(string roleName)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role.Name == roleName)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }
}