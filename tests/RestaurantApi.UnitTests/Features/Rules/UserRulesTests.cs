using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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
    
    #region UserShouldExist401

    [Fact]
    public async Task UserShouldExist401_WhenUserNotExist_ShouldThrowUnauthorizedException()
    {
        AppUser? user = null;

        Func<Task> act = async () => await _sut.UserShouldExist401(user);

        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Geçersiz kimlik bilgileri.");
    }
    
    [Fact]
    public async Task UserShouldExist401_WhenUserNotExist_ShouldNotThrowException()
    {
        AppUser user = new AppUser{Email = "mail@mail.com"};

        Func<Task> act = async () => await _sut.UserShouldExist401(user);

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

    #region ShouldUserVerified

    [Fact]
    public async Task ShouldUserVerified_WhenUserVerifiedTrue_ShouldReturnTrue()
    {
        var user = new AppUser() { Email = "test@mail.com", EmailConfirmed = true };

        Func<Task<bool>> act = async () => await _sut.ShouldUserVerified(user);
        
        var result = await act.Invoke();
        result.Should().BeTrue("Çünkü kullanıcının EmailConfirmed değeri true olarak set edildi.");
    }
    
    [Fact]
    public async Task ShouldUserVerified_WhenUserNotVerified_ShouldReturnFalse()
    {
        var user = new AppUser() { Email = "test@mail.com", EmailConfirmed = false };

        Func<Task<bool>> act = async () => await _sut.ShouldUserVerified(user);
        
        var result = await act.Invoke();
        result.Should().BeFalse("Çünkü kullanıcının EmailConfirmed değeri false olarak set edildi.");
    }

    #endregion
    
    #region ShouldUserTwoFactorEnable

    [Fact]
    public async Task ShouldUserTwoFactorEnable_WhenUserVerifiedTrue_ShouldReturnTrue()
    {
        var user = new AppUser() { Email = "test@mail.com", TwoFactorEnabled = true };

        Func<Task<bool>> act = async () => await _sut.ShouldUserTwoFactorEnable(user);
        
        var result = await act.Invoke();
        result.Should().BeTrue("Çünkü kullanıcının TwoFactorEnabled değeri true olarak set edildi.");
    }
    
    [Fact]
    public async Task ShouldUserTwoFactorEnable_WhenUserNotVerified_ShouldReturnFalse()
    {
        var user = new AppUser() { Email = "test@mail.com", TwoFactorEnabled = false };

        Func<Task<bool>> act = async () => await _sut.ShouldUserTwoFactorEnable(user);
        
        var result = await act.Invoke();
        result.Should().BeFalse("Çünkü kullanıcının TwoFactorEnabled değeri false olarak set edildi.");
    }

    #endregion

    #region ShouldUserNotVerified

    [Fact]
    public async Task ShouldUserNotVerified_ShouldReturnTrue_WhenUserVerified()
    {
        var user = new AppUser() { EmailConfirmed = true };

        Func<Task> act = async () => await _sut.ShouldUserNotVerified(user);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("Eposta adresiniz zaten doğrulanmış.");
    }
    
    [Fact]
    public async Task ShouldUserNotVerified_ShouldNotThrowAnything_WhenUserNotVerified()
    {
        var user = new AppUser() { EmailConfirmed = false };

        Func<Task> act = async () => await _sut.ShouldUserNotVerified(user);

        await act.Should().NotThrowAsync();
    }
    
    #endregion

    #region ShouldUserVerifiedSucceded

    [Fact]
    public async Task ShouldUserVerifiedSucceded_ShouldThrowBadRequestError_WhenVerificationFailed()
    {
        var identityError = new IdentityError 
        { 
            Code = "InvalidToken", 
            Description = "Geçersiz veya süresi dolmuş doğrulama kodu." 
        };
        var failedResult = IdentityResult.Failed(identityError);

        Func<Task> act = async () => await _sut.ShouldUserVerifiedSucceded(failedResult);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Geçersiz veya süresi dolmuş doğrulama kodu.");
    }
    
    [Fact]
    public async Task ShouldUserVerifiedSucceded_ShouldCompleteSuccessfully_WhenIdentityResultIsSuccess()
    {
        var successResult = IdentityResult.Success;

        Func<Task> act = async () => await _sut.ShouldUserVerifiedSucceded(successResult);

        await act.Should().NotThrowAsync();
    }

    #endregion
}