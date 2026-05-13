using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid userId);
    Task<IList<string>> GetUserRolesAsync(AppUser user);
}