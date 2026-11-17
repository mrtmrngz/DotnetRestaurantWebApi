using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Roles.Commands;

namespace RestaurantApi.UnitTests.Features.Roles;

// Rol bilgisi güncelleme komutunun şekil doğrulaması: rol id route'tan gelir ve zorunludur,
// rol adı zorunludur ve AppRole.Name kolonuyla aynı üst sınırı (256 karakter) aşamaz.
public class UpdateRoleInfoCommandValidatorTests
{
    private const string RoleIdRequiredMessage = "Role id zorunludur.";
    private const string RoleNameRequiredMessage = "Rol adı zorunludur.";
    private const string RoleNameMaxLengthMessage = "Rol adı en fazla 256 karakter olabilir.";

    private readonly UpdateRoleInfoCommandValidator _sut = new();

    #region Helper Methods

    private static UpdateRoleInfoCommand CreateValidCommand(string roleName = "KITCHEN")
    {
        return new UpdateRoleInfoCommand(roleName)
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
    [InlineData("KITCHEN")]
    [InlineData("admin")]
    [InlineData("Depo Sorumlusu")]
    public void Validate_WhenRoleNameIsFilled_ShouldNotHaveRoleNameValidationError(string roleName)
    {
        var result = _sut.TestValidate(CreateValidCommand(roleName));

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenRoleNameIsAtTheMaximumLength_ShouldNotHaveValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidCommand(new string('A', 256)));

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Role Id Tests

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldHaveRoleIdRequiredError()
    {
        var command = CreateValidCommand() with { RoleId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage(RoleIdRequiredMessage);
    }

    #endregion

    #region Role Name Tests

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldHaveRoleNameRequiredError()
    {
        var command = new UpdateRoleInfoCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldNotThrowException()
    {
        var command = new UpdateRoleInfoCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        Action act = () => _sut.TestValidate(command);

        act.Should().NotThrow("Null ad doğrulama hatası döndürmelidir, istisna fırlatmamalıdır");
    }

    [Fact]
    public void Validate_WhenRoleNameIsEmpty_ShouldHaveRoleNameRequiredError()
    {
        var result = _sut.TestValidate(CreateValidCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsWhitespace_ShouldHaveRoleNameRequiredError()
    {
        var result = _sut.TestValidate(CreateValidCommand("   "));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsLongerThanTheColumn_ShouldHaveMaxLengthError()
    {
        var result = _sut.TestValidate(CreateValidCommand(new string('A', 257)));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameMaxLengthMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldReturnOnlyTheRequiredError()
    {
        var command = new UpdateRoleInfoCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        var error = result.Errors.Should().ContainSingle(
            "null ad yalnızca zorunluluk hatası üretmelidir (uzunluk kontrolü tetiklenmemelidir)").Subject;

        error.PropertyName.Should().Be("Name");
        error.ErrorMessage.Should().Be(RoleNameRequiredMessage);
        error.ErrorCode.Should().Be("NotEmptyValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    #endregion

    #region Field Isolation Tests

    [Fact]
    public void Validate_WhenOnlyRoleIdIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { RoleId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.Errors.Should().HaveCount(1, "yalnızca rol id hatası dönmelidir");
    }

    [Fact]
    public void Validate_WhenOnlyRoleNameIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var result = _sut.TestValidate(CreateValidCommand(string.Empty));

        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
        result.Errors.Should().HaveCount(1, "yalnızca rol adı hatası dönmelidir");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreInvalid_ShouldHaveTwoValidationErrors()
    {
        var command = new UpdateRoleInfoCommand(string.Empty)
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleId).WithErrorMessage(RoleIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage(RoleNameRequiredMessage);

        result.Errors.Should().HaveCount(2, "iki alan da geçersiz olduğu için iki hata dönmelidir");
    }

    #endregion
}
