using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class RefreshTokenRepository: IRefreshTokenRepository
{

    private readonly ApiContext _context;

    public RefreshTokenRepository(ApiContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task AddAndRotateAsync(RefreshToken token)
    {
        await _context.RefreshTokens
            .Where(rt => rt.UserId == token.UserId && !rt.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken> GetAsync(string token)
    {
        var data = await _context.RefreshTokens
            .Where(x => x.Token == token && x.ExpiresAt > DateTime.UtcNow && !x.IsRevoked)
            .FirstOrDefaultAsync();

        return data;
    }
}