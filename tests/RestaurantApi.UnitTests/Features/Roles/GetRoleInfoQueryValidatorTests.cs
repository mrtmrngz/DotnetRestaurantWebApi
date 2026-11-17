using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;

namespace RestaurantApi.UnitTests.Features.Roles;

// Rol bilgisi sorgusunun şekil doğrulaması: rol ID zorunludur ve boş olamaz.
// Bu doğrulama ek bir güvenlik katmanıdır; route kısıtı sayesinde normal akışta istemciye hata dönmez.
public class GetRoleInfoQueryValidatorTests
{
    private const string RoleIdRequiredMessage = "Rol ID bilgisi zorunludur ve boş olamaz.";

    private readonly GetRoleInfoQueryValidator _sut = new();

    #region Helper Methods

    private static GetRoleInfoQuery CreateValidQuery()
    {
        return new GetRoleInfoQuery
        {
            RoleId = Guid.NewGuid()
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
    public void Validate_WhenRoleIdIsFilled_ShouldNotHaveRoleIdValidationError(string roleId)
    {
        var query = new GetRoleInfoQuery
        {
            RoleId = Guid.Parse(roleId)
        };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    #endregion

    #region Role Id Tests

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldHaveRoleIdRequiredError()
    {
        var query = new GetRoleInfoQuery
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage(RoleIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldReturnOnlyTheRequiredError()
    {
        var query = new GetRoleInfoQuery
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(query);

        var error = result.Errors.Should().ContainSingle(
            "boş rol ID yalnızca zorunluluk hatası üretmelidir").Subject;

        error.PropertyName.Should().Be("RoleId");
        error.ErrorMessage.Should().Be(RoleIdRequiredMessage);
        error.ErrorCode.Should().Be("NotEmptyValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    #endregion
}
