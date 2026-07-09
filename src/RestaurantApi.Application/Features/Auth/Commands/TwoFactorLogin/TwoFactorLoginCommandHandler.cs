using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Commands.TwoFactorLogin;

public class TwoFactorLoginCommandHandler: IRequestHandler<TwoFactorLoginCommand, LoginResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<TwoFactorLoginCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IAuthService _authService;

    public TwoFactorLoginCommandHandler(ICacheService cacheService, ILogger<TwoFactorLoginCommandHandler> logger, IUserRepository userRepository, UserRules userRules, IAuthService authService)
    {
        _cacheService = cacheService;
        _logger = logger;
        _userRepository = userRepository;
        _userRules = userRules;
        _authService = authService;
    }

    public async Task<LoginResponse> Handle(TwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await FindAndValidateUser(request.Otp);

        await _userRules.ShouldUserNotLockedOut(await _userRepository.IsLockedOutAsync(user!, cancellationToken), user.Email!);

        await EnsureOtpIsValid(user, request.Otp, cancellationToken);

        await ResetFailedCountAndRemoveCache(user, request.Otp, cancellationToken);

        return await _authService.HandleSuccessfulLoginAsync(user, user.Email!);
    }

    #region TwoFactorLoginHelperMethods

    private async Task<AppUser> FindAndValidateUser(string otp)
    {
        var cacheKey = CacheKeys.OtpToken(otp, "twoFactorAuth");
        var userId = await _cacheService.GetAsync<string>(cacheKey);

        await _userRules.ShouldUserIdExistOnCache(userId);

        var user = await _userRepository.GetByIdTrackingAsync(Guid.Parse(userId!));

        await _userRules.TwoFactorUserShouldExist(user, userId!);

        return user!;
    }

    private async Task EnsureOtpIsValid(AppUser user, string otp, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {Email} otp doğrulaması yapılıyor.", user.Email);
        var isOtpValid = await _userRepository.VerifyTwoFactorTokenAsync(user, otp, cancellationToken);

        if (!isOtpValid)
        {
            _logger.LogWarning("Kullanıcı {email} otp kodu {otp} hatalı.", user.Email, otp);
            
            await _userRepository.AccessFailedAsync(user, cancellationToken);

            await _userRules.ShouldUserNotLockedOut(await _userRepository.IsLockedOutAsync(user!, cancellationToken), user.Email!);

            throw new BadRequestException("Girdiğiniz doğrulama kodu hatalı veya süresi dolmuş.");
        }
    }

    private async Task ResetFailedCountAndRemoveCache(AppUser user, string otp, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcının hatalı giriş sayısı sıfırlanıyor.");
        await _userRepository.ResetAccessFailedCountAsync(user, cancellationToken);
        var cacheKey = CacheKeys.OtpToken(otp, "twoFactorAuth");
        await _cacheService.RemoveAsync(cacheKey);
    }

    #endregion
}