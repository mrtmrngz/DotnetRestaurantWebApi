
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
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
}