using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Infrastructure.Services;

public class AuthTokenService: IAuthTokenService
{
    private readonly ILogger<AuthTokenService> _logger;
    private readonly IGenerateRefreshToken _generateRefreshToken;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthTokenService(ILogger<AuthTokenService> logger, IGenerateRefreshToken generateRefreshToken, IUserRepository userRepository, ITokenService tokenService)
    {
        _logger = logger;
        _generateRefreshToken = generateRefreshToken;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<JwtAndRefreshTokenResult> CreateRefreshAndAccessTokenService(AppUser user)
    {
        _logger.LogInformation("Kullanıcı giriş yapabilir, tokenlar oluşturuluyor...");

        var refreshToken = await _generateRefreshToken.CreateRefreshToken(user.Id);

        IList<String> roles = await _userRepository.GetUserRolesAsync(user);

        // create access token

        var accessToken = await _tokenService.CreateAccessToken(user, roles);

        // return tokens

        _logger.LogInformation("Tokenlar oluşturuldu...");

        return new JwtAndRefreshTokenResult { AccessToken = accessToken, RefreshToken = refreshToken.Token };
    }
}