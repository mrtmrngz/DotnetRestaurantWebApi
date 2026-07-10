
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
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

    public async Task<AppUser?> GetByIdTrackingAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
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
    
    public async Task<AppUser?> FindByEmailAsyncTracking(string email, CancellationToken cancellationToken)
    {
        return await _userManager.Users
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
    
    public async Task<string> GenerateTwoFactorTokenAsync(AppUser user)
    {
        return await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
    }

    public async Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task UpdateSecurityStampAsync(AppUser user)
    {
        await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string otp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.VerifyTwoFactorTokenAsync(user, "Email", otp);
    }

    public async Task<bool> IsLockedOutAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.IsLockedOutAsync(user);
    }

    public async Task AccessFailedAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userManager.AccessFailedAsync(user);
    }

    public async Task ResetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }
}