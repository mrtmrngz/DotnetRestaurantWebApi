using System.Text;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordApply;

public class ForgotPasswordApplyCommandHandler: IRequestHandler<ForgotPasswordApplyCommand, BaseResponse>
{
    private readonly ILogger<ForgotPasswordApplyCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUserRepository _userRepository;
    private readonly UserRules _userRules;

    public ForgotPasswordApplyCommandHandler(ILogger<ForgotPasswordApplyCommandHandler> logger, ICacheService cacheService, IUserRepository userRepository, UserRules userRules)
    {
        _logger = logger;
        _cacheService = cacheService;
        _userRepository = userRepository;
        _userRules = userRules;
    }

    public async Task<BaseResponse> Handle(ForgotPasswordApplyCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ForgotPasswordKey(request.Token);
        var user = await FindAndValidateUser(request.Token, cacheKey);

        await ChangePasswordAndInvalidateCacheAsync(user, request.Token, request.Password, cacheKey, cancellationToken);
        
        return new BaseResponse
        {
            Message = "Şifreniz başarılı bir şekilde sıfırlandı, lütfen giriş yapınız.",
            Code = Codes.PASSWORD_RESET_SUCCESS
        };
    }
    
    #region Forgot Password Apply Helper Methods

    private async Task<AppUser> FindAndValidateUser(string token, string cacheKey)
    {
        _logger.LogInformation("Kullanıcı id rediste aranıyor: {Token}", token);
        
        var userId = await _cacheService.GetAsync<string>(cacheKey);
        await _userRules.ShouldUserIdExistOnCacheForgotPasswordApply(userId);

        _logger.LogInformation("Veri tabanında kullanıcı aranıyor: {UserId}", userId);
        var user = await _userRepository.GetByIdTrackingAsync(Guid.Parse(userId!));
        await _userRules.UserShouldExist404(user);

        return user!;
    }

    private async Task ChangePasswordAndInvalidateCacheAsync(AppUser user, string rawToken, string newPassword, string cacheKey, CancellationToken cancellationToken)
    {
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(rawToken));
        
        _logger.LogInformation("Kullanıcının parolası değişiyor: {Email}", user.Email);
        
        var result = await _userRepository.ResetPasswordAsync(user, decodedToken, newPassword, cancellationToken);

        await _userRules.ShouldUserPasswordChangedSuccessfully(result);
        
        _logger.LogInformation("Kullanıcının parolası başarılı bir şekilde değiştir, token redisten kaldırılıyor: {Email}", user.Email);
        await _cacheService.RemoveAsync(cacheKey);
    }

    #endregion
}