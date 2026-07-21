using Microsoft.AspNetCore.Identity;
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

    public Task ShouldUserNotVerified(AppUser user)
    {
        if (user!.EmailConfirmed)
        {
            _logger.LogInformation("Kullanıcının mail adresi zaten doğrulanmış durumda: {email}", user!.Email);
            throw new ConflictException("Eposta adresiniz zaten doğrulanmış.");
        }

        return Task.CompletedTask;
    }

    public Task ShouldUserVerifiedSucceded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ??
                             "Geçersiz veya süresi dolmuş doğrulama kodu.";
            _logger.LogError("Token doğrulanmadı: {Error}", firstError);
            throw new BadRequestException(firstError);
        }

        return Task.CompletedTask;
    }

    public Task ShouldUserIdExistOnCache(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "2FA login denemesi başarısız: Sağlanan OTP koduna ait aktif bir oturum Redis'te bulunamadı.");
            throw new UnauthorizedException("İki faktörlü kimlik doğrulama oturum süresi sona erdi, lütfen tekrar giriş yapınız.");
        }

        return Task.CompletedTask;
    }
    
    public Task TwoFactorUserShouldExist(AppUser? user, string userId)
    {
        if (user == null)
        {
            _logger.LogError("❌ 2FA login kritik hata: Redis'te UserId ({UserId}) var ancak veritabanında bu kullanıcı bulunamadı!", userId);

            throw new UnauthorizedException("Kimlik doğrulama oturumu geçersiz veya süresi dolmuş.");
        }

        return Task.CompletedTask;
    }

    public Task ShouldUserNotLockedOut(bool lockStatus, string email)
    {
        if (lockStatus)
        {
            _logger.LogWarning("❌ Kullanıcı ({Email}) kilitli hesapla 2FA denemesi yaptı.", email);
            throw new BadRequestException("Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlenmiştir.");
        }

        return Task.CompletedTask;
    }
    
    public Task ShouldUserIdExistOnCacheForgotPasswordApply(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Parola Değiştirme başarısız: Sağlanan token'a ait aktif bir oturum Redis'te bulunamadı.");
            throw new UnauthorizedException("Parola değiştirmek için size sağlanan token süresi sona erdi, lütfen tekrar deneyiniz.");
        }

        return Task.CompletedTask;
    }

    public Task ShouldUserPasswordChangedSuccessfully(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ??
                             "Geçersiz veya süresi dolmuş parola resetleme kodu.";
            _logger.LogError("Token doğrulanmadı: {Error}", firstError);
            throw new BadRequestException(firstError);
        }

        return Task.CompletedTask;
    }

    public Task ShouldUserExistBool404(bool isExist)
    {
        if (!isExist)
        {
            _logger.LogError("❌ Kullanıcı bulunamadı.");
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        return Task.CompletedTask;
    }
}