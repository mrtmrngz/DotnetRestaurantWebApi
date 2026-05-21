using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Rules.UserRules;

public class UserRules
{
    private readonly ILogger<UserRules> _logger;

    public UserRules(ILogger<UserRules> logger)
    {
        _logger = logger;
    }

    public Task UserShouldExist404(AppUser? user)
    {
        if (user == null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        return Task.CompletedTask;
    }
    
    public Task UserShouldExist401(AppUser? user)
    {
        if (user == null)
        {
            throw new UnauthorizedException("Geçersiz kimlik bilgileri.");
        }

        return Task.CompletedTask;
    }
    
    public Task ShouldUserNotExist(AppUser? user)
    {
        if (user is not null)
        {
            _logger.LogError("❌ Aynı mail adresine sahip kullanıcı bulundu.");
            throw new ConflictException("Aynı mail adresi ile kullanıcı bulunuyor.");
        }

        return Task.CompletedTask;
    }

    public async Task<bool> ShouldUserVerified(AppUser user)
    {
        return user.EmailConfirmed;
    }

    public async Task<bool> ShouldUserTwoFactorEnable(AppUser user)
    {
        return user.TwoFactorEnabled;
    }
}