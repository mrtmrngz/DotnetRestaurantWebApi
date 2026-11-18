using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Roles.Commands;

namespace RestaurantApi.UnitTests.Features.Roles;

// Rol silme komutunun şekil doğrulaması: rol ID zorunludur ve boş olamaz.
// Bu doğrulama ek bir güvenlik katmanıdır; route kısıtı sayesinde normal akışta istemciye hata dönmez.
public class RemoveRoleCommandValidatorTests
{
    private const string RoleIdRequiredMessage = "Rol ID bilgisi zorunludur ve boş olamaz.";

    private readonly RemoveRoleCommandValidator _sut = new();

    #region Helper Methods

    private static RemoveRoleCommand CreateValidCommand()
    {
        return new RemoveRoleCommand
        {
            RoleId = Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public void Validate_WhenRoleIdIsFilled_ShouldNotHaveRoleIdValidationError(string roleId)
    {
        var command = new RemoveRoleCommand
        {
            RoleId = Guid.Parse(roleId)
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    #endregion

    #region Role Id Tests

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldHaveRoleIdRequiredError()
    {
        var command = new RemoveRoleCommand
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage(RoleIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldReturnOnlyTheRequiredError()
    {
        var command = new RemoveRoleCommand
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(command);

        var error = result.Errors.Should().ContainSingle(
            "boş rol ID yalnızca zorunluluk hatası üretmelidir").Subject;

        error.PropertyName.Should().Be("RoleId");
        error.ErrorMessage.Should().Be(RoleIdRequiredMessage);
        error.ErrorCode.Should().Be("NotEmptyValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    #endregion
}
