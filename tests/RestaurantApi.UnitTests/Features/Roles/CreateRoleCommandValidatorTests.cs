using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Roles.Commands;

namespace RestaurantApi.UnitTests.Features.Roles;

// Rol oluşturma komutunun şekil doğrulaması: rol adı zorunludur, boş/yalnız boşluk olamaz
// ve AppRole.Name kolonuyla aynı üst sınırı (256 karakter) aşamaz.
public class CreateRoleCommandValidatorTests
{
    private const string RoleNameRequiredMessage = "Rol adı zorunludur.";
    private const string RoleNameMaxLengthMessage = "Rol adı en fazla 256 karakter olabilir.";

    private readonly CreateRoleCommandValidator _sut = new();

    #region Helper Methods

    private string CreateRoleName(int length)
    {
        return new string('A', length);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(new CreateRoleCommand("KITCHEN"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("KITCHEN")]
    [InlineData("admin")]
    [InlineData("Depo Sorumlusu")]
    public void Validate_WhenRoleNameIsFilled_ShouldNotHaveRoleNameValidationError(string roleName)
    {
        var result = _sut.TestValidate(new CreateRoleCommand(roleName));

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenRoleNameIsAtTheMaximumLength_ShouldNotHaveValidationErrors()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(CreateRoleName(256)));

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Role Name Tests

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldHaveRoleNameRequiredError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(null!));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldNotThrowException()
    {
        Action act = () => _sut.TestValidate(new CreateRoleCommand(null!));

        act.Should().NotThrow("Null ad doğrulama hatası döndürmelidir, istisna fırlatmamalıdır");
    }

    [Fact]
    public void Validate_WhenRoleNameIsNull_ShouldReturnOnlyTheRequiredError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(null!));

        var error = result.Errors.Should().ContainSingle(
            "null ad yalnızca zorunluluk hatası üretmelidir (uzunluk kontrolü tetiklenmemelidir)").Subject;

        error.PropertyName.Should().Be("Name");
        error.ErrorMessage.Should().Be(RoleNameRequiredMessage);
        error.ErrorCode.Should().Be("NotEmptyValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Validate_WhenRoleNameIsEmpty_ShouldHaveRoleNameRequiredError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsWhitespace_ShouldHaveRoleNameRequiredError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand("   "));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsLongerThanTheColumn_ShouldHaveMaxLengthError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(CreateRoleName(257)));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(RoleNameMaxLengthMessage);
    }

    [Fact]
    public void Validate_WhenRoleNameIsTooLong_ShouldReturnOnlyTheMaxLengthError()
    {
        var result = _sut.TestValidate(new CreateRoleCommand(CreateRoleName(300)));

        var error = result.Errors.Should().ContainSingle(
            "yalnızca uzunluk hatası dönmelidir (zorunluluk kontrolü tetiklenmemelidir)").Subject;

        error.ErrorMessage.Should().Be(RoleNameMaxLengthMessage);
    }

    #endregion
}
