using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IRoleRepository
{
    IQueryable<AppRole> GetAllAsQueryable();

    // Rol id'sine göre, role bağlı kullanıcı sayıları (AspNetUserRoles join tablosundan).
    Task<Dictionary<Guid, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken);
}
