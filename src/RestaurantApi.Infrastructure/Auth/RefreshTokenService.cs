using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Files.Dtos.RefreshTokenDtos;
using RestaurantApi.Application.Features.Rules.RefreshTokenRules;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Infrastructure.Auth;

public class RefreshTokenService : IGenerateRefreshToken
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICacheService _cacheService;
    private readonly RefreshTokenRules _refreshTokenRules;

    public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, ICacheService cacheService, RefreshTokenRules refreshTokenRules)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _cacheService = cacheService;
        _refreshTokenRules = refreshTokenRules;
    }

    public async Task<GenerateRefreshTokenDto> GenerateRefreshToken(string token)
    {
        Guid? userId = null;

        var cacheUserId = await _cacheService.GetAsync<string>(CacheKeys.RefreshToken(token));

        if (string.IsNullOrEmpty(cacheUserId))
        {
            var dbToken = await _refreshTokenRepository.GetAsync(token);
            await _refreshTokenRules.TokenShouldExist(dbToken);

            userId = dbToken!.UserId;
        }
        else
        {
            userId = Guid.Parse(cacheUserId);
        }

        await _cacheService.RemoveAsync(CacheKeys.RefreshToken(token));

        var newToken = $"{Guid.NewGuid()}-{Guid.NewGuid()}";

        var tokenModel = new RefreshToken
        {
            Token = newToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UserId = userId.Value,
            IsRevoked = false,
        };

        await _refreshTokenRepository.AddAndRotateAsync(tokenModel);

        await _cacheService.SetAsync(CacheKeys.RefreshToken(newToken), userId, TimeSpan.FromDays(7));

        return new GenerateRefreshTokenDto { UserId = userId.Value, Token = newToken };
    }
}