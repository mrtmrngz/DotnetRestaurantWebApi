using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.ProfileRules;

public class ProfileRules
{
    private readonly ILogger<ProfileRules> _logger;

    public ProfileRules(ILogger<ProfileRules> logger)
    {
        _logger = logger;
    }


    public Task ShouldPasswordsNotMatchChangePassword(PasswordVerificationResult passwordResult)
    {
        if (passwordResult == PasswordVerificationResult.Success)
        {
            _logger.LogWarning("Kullanıcının eski parolası ile yeni parolası çakıştı.");
            throw new BadRequestException("Eski parola ile yeni parola aynı olamaz.");
        }


        return Task.CompletedTask;
    }


    public Task ShouldPasswordChangeSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ??
                             "Parolanız değiştirilemedi, daha sonra tekrar deneyiniz.";
            _logger.LogError("Parola değiştirme hatası: {Error}", firstError);
            throw new BadRequestException(firstError);
        }

        return Task.CompletedTask;
    }

    public Task ShouldOldPasswordCorrectPasswordChange(PasswordVerificationResult result)
    {
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Kullanıcının eski parolasını yanlış girdi.");
            throw new BadRequestException("Eski parolanızı yanlış girdiniz.");
        }


        return Task.CompletedTask;
    }
}