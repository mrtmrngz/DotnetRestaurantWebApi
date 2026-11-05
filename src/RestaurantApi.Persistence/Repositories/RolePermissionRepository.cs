using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class RolePermissionRepository: IRolePermissionRepository
{
    private readonly ApiContext _context;

    public RolePermissionRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<RolePermission> GetAllAsQueryable()
    {
        return _context.RolePermissions.AsQueryable();
    }

    public void AddRange(IEnumerable<RolePermission> rolePermissions)
    {
        _context.RolePermissions.AddRange(rolePermissions);
    }

    public void RemoveRange(IEnumerable<RolePermission> rolePermissions)
    {
        _context.RolePermissions.RemoveRange(rolePermissions);
    }
}