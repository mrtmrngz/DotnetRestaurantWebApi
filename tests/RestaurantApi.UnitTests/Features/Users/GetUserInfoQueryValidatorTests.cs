using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;

namespace RestaurantApi.UnitTests.Features.Users;

// Kullanıcı bilgisi sorgusunun şekil doğrulaması: kullanıcı ID zorunludur ve boş olamaz.
// Bu doğrulama ek bir güvenlik katmanıdır; tip ve route kısıtı sayesinde normal akışta istemciye hata dönmez.
public class GetUserInfoQueryValidatorTests
{
    private const string UserIdRequiredMessage = "Kullanıcı ID bilgisi zorunludur ve boş olamaz.";

    private readonly GetUserInfoQueryValidator _sut = new();

    #region Helper Methods

    private static GetUserInfoQuery CreateValidQuery()
    {
        return new GetUserInfoQuery
        {
            UserId = Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public void Validate_WhenUserIdIsFilled_ShouldNotHaveUserIdValidationError(string userId)
    {
        var query = new GetUserInfoQuery
        {
            UserId = Guid.Parse(userId)
        };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region User Id Tests

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var query = new GetUserInfoQuery
        {
            UserId = Guid.Empty
        };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldReturnOnlyTheRequiredError()
    {
        var query = new GetUserInfoQuery
        {
            UserId = Guid.Empty
        };

        var result = _sut.TestValidate(query);

        var error = result.Errors.Should().ContainSingle(
            "boş kullanıcı ID yalnızca zorunluluk hatası üretmelidir").Subject;

        error.PropertyName.Should().Be("UserId");
        error.ErrorMessage.Should().Be(UserIdRequiredMessage);
        error.ErrorCode.Should().Be("NotEmptyValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    #endregion
}
