using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IRolePermissionRepository
{
    IQueryable<RolePermission> GetAllAsQueryable();
    void AddRange(IEnumerable<RolePermission> rolePermissions);
    void RemoveRange(IEnumerable<RolePermission> rolePermissions);
}