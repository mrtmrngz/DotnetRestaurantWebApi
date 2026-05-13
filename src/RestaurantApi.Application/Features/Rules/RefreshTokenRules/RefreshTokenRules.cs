using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Rules.RefreshTokenRules;

public class RefreshTokenRules
{
    public Task TokenShouldExist(RefreshToken token)
    {
        if (token == null)
        {
            throw new UnauthorizedException("Oturum süreniz sona erdi, lütfen tekrar giriş yapınız.");
        }

        return Task.CompletedTask;
    }
}