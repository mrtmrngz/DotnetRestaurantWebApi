using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task AddAndRotateAsync(RefreshToken token);
    Task<RefreshToken> GetAsync(string token);
}