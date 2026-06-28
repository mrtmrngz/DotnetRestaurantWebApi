using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ILogger<AuthService> logger,
        IAuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
        _logger = logger;
    }
    
    public async Task<LoginResponse> HandleSuccessfulLoginAsync(AppUser user, string email)
    {
        var tokens = await _authTokenService.CreateRefreshAndAccessTokenService(user!);

        _logger.LogInformation("Giriş başarılı: {email}", email);

        return new LoginResponse()
        {
            Code = Codes.LOGIN_SUCCESS,
            Message = "Giriş başarılı.",
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
        };
    }
}