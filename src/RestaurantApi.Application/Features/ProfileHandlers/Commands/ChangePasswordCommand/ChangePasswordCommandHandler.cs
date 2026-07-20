using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ProfileRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, BaseResponse>
{
    private readonly ILogger<ChangePasswordCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ProfileRules _profileRules;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _uow;

    public ChangePasswordCommandHandler(ILogger<ChangePasswordCommandHandler> logger, ICacheService cacheService,
        IUserRepository userRepository, UserRules userRules, IPasswordHasher<AppUser> passwordHasher,
        ProfileRules profileRules, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork uow)
    {
        _logger = logger;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _userRules = userRules;
        _passwordHasher = passwordHasher;
        _profileRules = profileRules;
        _refreshTokenRepository = refreshTokenRepository;
        _uow = uow;
    }

    public async Task<BaseResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kullanıcı {Id} parola değiştirme isteği yaptı.", request.UserId);

        // find and validate user
        var user = await _userRepository.GetByIdTrackingAsync(request.UserId);
        await _userRules.UserShouldExist404(user);

        await _uow.BeginTransaction(cancellationToken);

        // change password
        await ValidateAndChangePassword(user!, request.OldPassword, request.NewPassword, cancellationToken);

        // find active refresh token and revoke it
        var tokenModel = await _refreshTokenRepository.GetUserActiveToken(request.UserId);

        if (tokenModel is not null)
        {
            await _refreshTokenRepository.RevokeRefreshToken(tokenModel.Token);
        }

        await _uow.CommitTransactionAsync(cancellationToken);

        // remove refresh token, get me, profile from cache 
        await HandleClearCache(user!.Id, tokenModel?.Token);

        _logger.LogInformation("Şifre değiştirme işleminde tüm adımlar başarılı bir şekilde tamamlandı.");

        return new BaseResponse
        {
            Message = "Parolanız başarıyla değiştirildi, lütfen tekrar giriş yapınız.",
            Code = Codes.PASSWORD_RESET_SUCCESS
        };
    }

    #region Change Password Handler Helper Methods

    private async Task ValidateAndChangePassword(AppUser user, string oldPassword, string newPassword,
        CancellationToken ctx)
    {
        // old password validate
        await _profileRules.ShouldOldPasswordCorrectPasswordChange(
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, oldPassword)
        );

        
        await _profileRules.ShouldPasswordsNotMatchChangePassword(
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, newPassword));

        await _profileRules.ShouldPasswordChangeSuccess(
            await _userRepository.ChangePasswordAsync(user, oldPassword, newPassword, ctx)
        );

        await _userRepository.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Kullanıcının şifresi başarılı bir şekilde değişti. {Email}", user.Email);
    }

    private async Task HandleClearCache(Guid userId, string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            var tokenCacheKey = CacheKeys.RefreshToken(token);
            await _cacheService.RemoveAsync(tokenCacheKey);
        }

        var userCacheKey = CacheKeys.GetMeKey(userId.ToString());
        await _cacheService.RemoveAsync(userCacheKey);

        var profileCacheKey = CacheKeys.ProfileInfoKey(userId.ToString());
        await _cacheService.RemoveAsync(profileCacheKey);
    }

    #endregion
}