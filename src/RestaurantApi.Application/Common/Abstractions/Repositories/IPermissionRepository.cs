using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IPermissionRepository
{
    Task<List<string>> GetPermissionsByRoleNameAsync(string role);
    IQueryable<Permission> GetAllAsQueryable();
}