
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Repositories;

public class UserRepository: IUserRepository
{
    private readonly UserManager<AppUser> _userManager;

    public UserRepository(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AppUser?> GetByIdAsync(Guid userId)
    {
        return await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<IList<string>> GetUserRolesAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return roles.ToList();
    }

    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email || u.NormalizedEmail == email, cancellationToken);
    }

    public async Task<IdentityResult> CreateAsync(AppUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task AddToRoleAsync(AppUser user, string roleName)
    {
        await _userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }
}