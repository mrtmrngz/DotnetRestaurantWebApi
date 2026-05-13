using RestaurantApi.Application.Features.Files.Dtos.RefreshTokenDtos;

namespace RestaurantApi.Application.Common.Abstractions;

public interface IGenerateRefreshToken
{
    Task<GenerateRefreshTokenDto> GenerateRefreshToken(string token);
}