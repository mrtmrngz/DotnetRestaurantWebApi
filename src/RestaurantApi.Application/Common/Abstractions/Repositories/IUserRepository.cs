using Microsoft.AspNetCore.Identity;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid userId);
    Task<IList<string>> GetUserRolesAsync(AppUser user);
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IdentityResult> CreateAsync(AppUser user, string password);
    Task AddToRoleAsync(AppUser user, string roleName);
    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
}