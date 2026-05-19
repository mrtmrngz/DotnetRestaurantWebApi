using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.UnitTests.Features.Rules;

public class UserRulesTests
{
    private readonly ILogger<UserRules> _loggerMock = Substitute.For<ILogger<UserRules>>();
    private readonly UserRules _sut;

    public UserRulesTests()
    {
        _sut = new UserRules(_loggerMock);
    }

    #region UserShouldExist404 Tests

    [Fact]
    public async Task UserShouldExist404_WhenUserNotExist_ShouldThrowNotFoundException()
    {
        AppUser? user = null;

        Func<Task> act = async () => await _sut.UserShouldExist404(user);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Kullanıcı bulunamadı.");
    }
    
    [Fact]
    public async Task UserShouldExist404_WhenUserExist_ShouldNotThrowException()
    {
        AppUser user = new AppUser{Email = "mail@mail.com"};

        Func<Task> act = async () => await _sut.UserShouldExist404(user);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ShouldUserNotExist Tests

    [Fact]
    public async Task ShouldUserNotExist_WhenUserExist_ShouldThrowConflictException()
    {
        AppUser user = new AppUser{Email = "mail@mail.com"};

        Func<Task> act = async () => await _sut.ShouldUserNotExist(user);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("Aynı mail adresi ile kullanıcı bulunuyor.");
    }
    
    [Fact]
    public async Task ShouldUserNotExist_WhenUserExist_ShouldNotThrowException()
    {
        AppUser? user = null;

        Func<Task> act = async () => await _sut.ShouldUserNotExist(user);

        await act.Should().NotThrowAsync();
    }

    #endregion
}