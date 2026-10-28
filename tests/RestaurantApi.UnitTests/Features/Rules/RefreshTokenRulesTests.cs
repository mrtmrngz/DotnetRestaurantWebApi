using FluentAssertions;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.RefreshTokenRules;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.UnitTests.Features.Rules;

public class RefreshTokenRulesTests
{
    private readonly RefreshTokenRules _sut = new();

    #region TokenShouldExist Tests

    [Fact]
    public async Task TokenShouldExist_WhenTokenNotExist_ShouldThrowUnauthorizedException()
    {
        RefreshToken? token = null;

        var act = async () => await _sut.TokenShouldExist(token);

        await act.Should().ThrowAsync<UnauthorizedException>(
                "Oturum bulunamadığı için kullanıcıdan tekrar giriş yapması istenmelidir")
            .WithMessage("Oturum süreniz sona erdi, lütfen tekrar giriş yapınız.");
    }

    [Fact]
    public async Task TokenShouldExist_WhenTokenExists_ShouldNotThrowAsync()
    {
        RefreshToken token = new()
        {
            Id = Guid.NewGuid(),
            Token = "refresh-token-value",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        var act = async () => await _sut.TokenShouldExist(token);

        await act.Should().NotThrowAsync("Oturum bulunduğu için hata fırlatılmamalıydı");
    }

    #endregion
}
