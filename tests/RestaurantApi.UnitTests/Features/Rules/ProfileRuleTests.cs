using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.ProfileRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class ProfileRuleTests
{
    private readonly ILogger<ProfileRules> _loggerMock = Substitute.For<ILogger<ProfileRules>>();
    private readonly ProfileRules _sut;

    public ProfileRuleTests()
    {
        _sut = new ProfileRules(_loggerMock);
    }

    #region ShouldPasswordsNotMatchChangePassword Tests

    [Fact]
    public async Task
        ShouldPasswordsNotMatchChangePassword_WhenPasswordVerificationResultSuccess_ShouldThrowBadRequestException()
    {
        var status = PasswordVerificationResult.Success;

        Func<Task> act = async () => await _sut.ShouldPasswordsNotMatchChangePassword(status);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Eski parola ile yeni parola aynı olamaz.");
    }

    [Fact]
    public async Task
        ShouldPasswordsNotMatchChangePassword_WhenPasswordVerificationResultFailed_ShouldNotThrowAsync()
    {
        var status = PasswordVerificationResult.Failed;

        Func<Task> act = async () => await _sut.ShouldPasswordsNotMatchChangePassword(status);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ShouldPasswordChangeSuccess Tests

    [Fact]
    public async Task ShouldPasswordChangeSuccess_WhenIdentityResultFailed_ShouldThrowBadRequestException()
    {
        var failedResult = IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordFailed",
            Description = "Parolanız değiştirilemedi, daha sonra tekrar deneyiniz."
        });

        Func<Task> act = async () => await _sut.ShouldPasswordChangeSuccess(failedResult);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Parolanız değiştirilemedi, daha sonra tekrar deneyiniz.");
    }
    
    [Fact]
    public async Task ShouldPasswordChangeSuccess_WhenIdentityResultSuccess_ShouldNotThrowAsync()
    {
        var result = IdentityResult.Success;

        Func<Task> act = async () => await _sut.ShouldPasswordChangeSuccess(result);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ShouldOldPasswordCorrectPasswordChange Tests

    [Fact]
    public async Task
        ShouldOldPasswordCorrectPasswordChange_WhenPasswordVerificationResultFailed_ShouldThrowBadRequestException()
    {
        var status = PasswordVerificationResult.Failed;

        Func<Task> act = async () => await _sut.ShouldOldPasswordCorrectPasswordChange(status);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Eski parolanızı yanlış girdiniz.");
    }
    
    [Fact]
    public async Task
        ShouldOldPasswordCorrectPasswordChange_WhenPasswordVerificationResultFailed_ShouldNotThrowAsync()
    {
        var status = PasswordVerificationResult.Success;

        Func<Task> act = async () => await _sut.ShouldOldPasswordCorrectPasswordChange(status);

        await act.Should().NotThrowAsync();
    }

    #endregion
}