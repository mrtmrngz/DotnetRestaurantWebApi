using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Infrastructure.Services;

public class AuthTokenService: IAuthTokenService
{
    private readonly ILogger<AuthTokenService> _logger;
    private readonly IGenerateRefreshToken _generateRefreshToken;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly UserRules _userRules;
    private readonly ICacheService _cacheService;

    public AuthTokenService(ILogger<AuthTokenService> logger, IGenerateRefreshToken generateRefreshToken, IUserRepository userRepository, ITokenService tokenService, UserRules userRules, ICacheService cacheService)
    {
        _logger = logger;
        _generateRefreshToken = generateRefreshToken;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _userRules = userRules;
        _cacheService = cacheService;
    }

    public async Task<JwtAndRefreshTokenResult> CreateRefreshAndAccessTokenService(AppUser user)
    {
        _logger.LogInformation("Kullanıcı giriş yapabilir, tokenlar oluşturuluyor...");

        var refreshToken = await _generateRefreshToken.CreateRefreshToken(user.Id);

        var accessToken = await FindRolesAndCreateAccessToken(user);

        return new JwtAndRefreshTokenResult { AccessToken = accessToken, RefreshToken = refreshToken.Token };
    }

    public async Task<JwtAndRefreshTokenResult> RefreshTokenAsyncService(string token)
    {
        _logger.LogInformation("Kullanıcının tokenleri yenileniyor...");
        var refreshToken = await _generateRefreshToken.GenerateRefreshToken(token);

        var user = await _userRepository.GetByIdAsync(refreshToken.UserId);

        await _userRules.UserShouldExist404(user);
        
        var accessToken = await FindRolesAndCreateAccessToken(user!);
        
        return new JwtAndRefreshTokenResult { AccessToken = accessToken, RefreshToken = refreshToken.Token };
    }

    private async Task<string> FindRolesAndCreateAccessToken(AppUser user)
    {

        var roleUserRoleCacheKey = CacheKeys.UserRoles(user.Id.ToString());
        // find roles
        var roleExistInCache = await _cacheService.GetOrInternalSetAsync(roleUserRoleCacheKey, async () =>
        { 
            IList<String> roles = await _userRepository.GetUserRolesAsync(user);
            return roles;
        }, TimeSpan.FromDays(7));

        // create access token

        var accessToken = await _tokenService.CreateAccessToken(user, roleExistInCache);

        // return tokens

        _logger.LogInformation("Tokenlar oluşturuldu...");

        return accessToken;
    }
}