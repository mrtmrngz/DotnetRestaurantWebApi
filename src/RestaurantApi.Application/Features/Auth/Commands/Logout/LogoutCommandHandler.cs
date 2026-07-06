using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ICacheService _cacheService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(ICacheService cacheService, IRefreshTokenRepository refreshTokenRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _cacheService = cacheService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenKey = CacheKeys.RefreshToken(request.Token);
        var userId = await _cacheService.GetAsync<string>(tokenKey);
        _logger.LogInformation("Kullanıcının {UserId} tokeni redisten siliniyor", userId ?? "Kullanıcının id'si yok");
        await _cacheService.RemoveAsync(tokenKey);
        if (!string.IsNullOrEmpty(userId))
        {
            var roleKey = CacheKeys.UserRoles(userId);
            _logger.LogInformation("Kullanıcının {UserId} rolleri redisten siliniyor",
                userId);

            await _cacheService.RemoveAsync(roleKey);
        }

        _logger.LogInformation("Kullanıcının tokeni geçersiz sayılıyor...");
        await _refreshTokenRepository.RevokeRefreshToken(request.Token);
    }
}