using Microsoft.AspNetCore.Identity;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid userId);
    Task<AppUser?> GetByIdTrackingAsync(Guid userId);
    Task<IList<string>> GetUserRolesAsync(AppUser user);
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<AppUser?> FindByEmailAsyncTracking(string email, CancellationToken cancellationToken);
    Task<IdentityResult> CreateAsync(AppUser user, string password);
    Task AddToRoleAsync(AppUser user, string roleName);
    Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
    Task<string> GenerateTwoFactorTokenAsync(AppUser user);
    Task<IdentityResult> ConfirmEmailAsync(AppUser user, string token);
    Task UpdateSecurityStampAsync(AppUser user);
    Task<bool> VerifyTwoFactorTokenAsync(AppUser user, string otp, CancellationToken cancellationToken);
    Task<bool> IsLockedOutAsync(AppUser user, CancellationToken cancellationToken);
    Task AccessFailedAsync(AppUser user, CancellationToken cancellationToken);
    Task ResetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken);
    Task<string> GeneratePasswordResetTokenAsync(AppUser user, CancellationToken cancellationToken);
    Task<IdentityResult> ResetPasswordAsync(AppUser user, string token, string newPassword, CancellationToken cancellationToken);
}