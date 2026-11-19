using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Users.Commands.UpdateUserRoleCommand;

namespace RestaurantApi.UnitTests.Features.Users;

public class UpdateUserRoleCommandValidatorTests
{
    private const string UserIdRequiredMessage = "Kullanıcı id boş olamaz";
    private const string RoleIdsRequiredMessage = "Rol id'leri zorunludur.";
    private const string AtLeastOneRoleMessage = "Kullanıcıya en az 1 rol atanmalıdır.";
    private const string EmptyRoleIdMessage = "Rol id boş olamaz.";
    private const string InvalidRoleIdMessage = "Geçersiz rol id formatı.";
    private const string DuplicateRoleMessage = "Rol listesinde tekrar eden değerler var.";

    private readonly UpdateUserRoleCommandValidator _sut = new();

    #region Helper Methods

    private static UpdateUserRoleCommand CreateValidCommand(params string[] roleIds)
    {
        return new UpdateUserRoleCommand(roleIds.Length == 0 ? new List<string> { Guid.NewGuid().ToString() } : roleIds)
        {
            UserId = Guid.NewGuid()
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
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Validate_WhenRoleIdsAreUniqueGuids_ShouldNotHaveValidationErrors(int roleCount)
    {
        var roleIds = Enumerable.Range(0, roleCount).Select(_ => Guid.NewGuid().ToString()).ToArray();

        var result = _sut.TestValidate(CreateValidCommand(roleIds));

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region User Id Tests

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    #endregion

    #region Role Ids Tests

    [Fact]
    public void Validate_WhenRoleIdsIsNull_ShouldHaveRoleIdsRequiredError()
    {
        var command = new UpdateUserRoleCommand(null!)
        {
            UserId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(RoleIdsRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdsIsNull_ShouldNotThrowException()
    {
        var command = new UpdateUserRoleCommand(null!)
        {
            UserId = Guid.NewGuid()
        };

        Action act = () => _sut.TestValidate(command);

        act.Should().NotThrow("Null liste doğrulama hatası döndürmelidir, istisna fırlatmamalıdır");
    }

    [Fact]
    public void Validate_WhenRoleIdsIsNull_ShouldReturnOnlyTheRequiredError()
    {
        var command = new UpdateUserRoleCommand(null!)
        {
            UserId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        var error = result.Errors.Should().ContainSingle(
            "null listede yalnızca zorunluluk hatası dönmelidir").Subject;

        error.PropertyName.Should().Be("RoleIds");
        error.ErrorCode.Should().Be("NotNullValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Validate_WhenRoleIdsIsEmpty_ShouldHaveAtLeastOneRoleError()
    {
        var command = CreateValidCommand() with { RoleIds = new List<string>() };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(AtLeastOneRoleMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdIsEmpty_ShouldHaveEmptyRoleIdError()
    {
        var result = _sut.TestValidate(CreateValidCommand(string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(EmptyRoleIdMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdIsNotAGuid_ShouldHaveInvalidRoleIdError()
    {
        var result = _sut.TestValidate(CreateValidCommand("not-a-guid"));

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(InvalidRoleIdMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdIsEmptyGuid_ShouldHaveInvalidRoleIdError()
    {
        var result = _sut.TestValidate(CreateValidCommand(Guid.Empty.ToString()));

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(InvalidRoleIdMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdsHasDuplicates_ShouldHaveDuplicateError()
    {
        var roleId = Guid.NewGuid().ToString();

        var result = _sut.TestValidate(CreateValidCommand(roleId, roleId));

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(DuplicateRoleMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdsHasDuplicatesInDifferentCasing_ShouldHaveDuplicateError()
    {
        var roleId = Guid.NewGuid().ToString();

        var result = _sut.TestValidate(CreateValidCommand(roleId, roleId.ToUpperInvariant()));

        result.ShouldHaveValidationErrorFor(x => x.RoleIds)
            .WithErrorMessage(DuplicateRoleMessage);
    }

    [Fact]
    public void Validate_WhenRoleIdsIsInvalid_ShouldReturnOnlyTheRelatedError()
    {
        var result = _sut.TestValidate(CreateValidCommand("not-a-guid"));

        result.Errors.Should().HaveCount(1, "yalnızca geçersiz rol id hatası dönmelidir");
    }

    #endregion

    #region Field Isolation Tests

    [Fact]
    public void Validate_WhenOnlyUserIdIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RoleIds);
        result.Errors.Should().HaveCount(1, "yalnızca kullanıcı id hatası dönmelidir");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreInvalid_ShouldHaveTwoValidationErrors()
    {
        var command = new UpdateUserRoleCommand(new List<string>())
        {
            UserId = Guid.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorMessage(UserIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds).WithErrorMessage(AtLeastOneRoleMessage);

        result.Errors.Should().HaveCount(2, "iki alan da geçersiz olduğu için iki hata dönmelidir");
    }

    #endregion
}
