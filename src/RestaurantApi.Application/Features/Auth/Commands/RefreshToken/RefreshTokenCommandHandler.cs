using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler: IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IAuthTokenService _authTokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(IAuthTokenService authTokenService, ILogger<RefreshTokenCommandHandler> logger)
    {
        _authTokenService = authTokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            _logger.LogWarning("Kullanıcının oturum süresi sona erdi...");
            throw new UnauthorizedException("Oturum süreniz dolmuş, lütfen tekrar giriş yapın.");
        }

        var tokens = await _authTokenService.RefreshTokenAsyncService(request.Token);
        
        return new LoginResponse { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken, Code = Codes.TOKEN_REFRESHED};
    }
}