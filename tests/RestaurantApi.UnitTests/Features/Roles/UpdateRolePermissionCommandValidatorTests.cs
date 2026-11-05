using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Roles.Commands;

namespace RestaurantApi.UnitTests.Features.Roles;

public class UpdateRolePermissionCommandValidatorTests
{
    private const string RoleIdRequiredMessage = "Role id zorunludur.";
    private const string PermissionIdsRequiredMessage = "İzin id'leri zorunludur.";
    private const string DuplicatePermissionMessage = "İzin listesinde tekrar eden değerler var.";
    private const string EmptyPermissionMessage = "İzin id boş olamaz.";

    private readonly UpdateRolePermissionCommandValidator _sut = new();

    #region Helper Methods

    private static UpdateRolePermissionCommand CreateValidCommand(IReadOnlyCollection<Guid>? permissionIds = null)
    {
        return new UpdateRolePermissionCommand(
            permissionIds ?? new List<Guid> { Guid.NewGuid(), Guid.NewGuid() })
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

    [Fact]
    public void Validate_WhenPermissionListIsEmpty_ShouldNotHaveValidationErrors()
    {
        // Boş liste geçerlidir: rolün tüm izinlerinin kaldırılması istenir.
        var result = _sut.TestValidate(CreateValidCommand(Array.Empty<Guid>()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    public void Validate_WhenPermissionListHasUniqueIds_ShouldNotHaveValidationErrors(int permissionCount)
    {
        var permissionIds = Enumerable.Range(0, permissionCount).Select(_ => Guid.NewGuid()).ToList();

        var result = _sut.TestValidate(CreateValidCommand(permissionIds));

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

    #region Permission Ids Tests

    [Fact]
    public void Validate_WhenPermissionIdsIsNull_ShouldHavePermissionIdsRequiredError()
    {
        var command = new UpdateRolePermissionCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PermissionIds)
            .WithErrorMessage(PermissionIdsRequiredMessage);
    }

    [Fact]
    public void Validate_WhenPermissionIdsIsNull_ShouldNotThrowException()
    {
        // Ayrı bir doğrulama zinciri kurulursa null liste üzerinde tekrar/boş kontrolü hata verir (NRE).
        var command = new UpdateRolePermissionCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        Action act = () => _sut.TestValidate(command);

        act.Should().NotThrow("Null liste doğrulama hatası döndürmelidir, istisna fırlatmamalıdır");
    }

    [Fact]
    public void Validate_WhenPermissionIdsIsNull_ShouldReturnOnlyTheRequiredError()
    {
        var command = new UpdateRolePermissionCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        var error = result.Errors.Should().ContainSingle(
            "null listede yalnızca zorunluluk hatası dönmelidir").Subject;

        error.PropertyName.Should().Be("PermissionIds");
        error.ErrorMessage.Should().Be(PermissionIdsRequiredMessage);
        error.ErrorCode.Should().Be("NotNullValidator", "hata sözleşmesi korunmalıdır");
        error.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Validate_WhenPermissionIdsHasDuplicates_ShouldHaveDuplicateError()
    {
        var duplicatedPermissionId = Guid.NewGuid();

        var command = CreateValidCommand(new List<Guid>
        {
            duplicatedPermissionId,
            Guid.NewGuid(),
            duplicatedPermissionId
        });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PermissionIds)
            .WithErrorMessage(DuplicatePermissionMessage);
    }

    [Fact]
    public void Validate_WhenPermissionIdsHasDuplicates_ShouldReturnOnlyTheDuplicateError()
    {
        var duplicatedPermissionId = Guid.NewGuid();

        var command = CreateValidCommand(new List<Guid> { duplicatedPermissionId, duplicatedPermissionId });

        var result = _sut.TestValidate(command);

        var error = result.Errors.Should().ContainSingle(
            "tekrar eden liste yalnızca tek hata üretmelidir (boş id kontrolü tetiklenmemelidir)").Subject;

        error.ErrorMessage.Should().Be(DuplicatePermissionMessage);
    }

    [Fact]
    public void Validate_WhenPermissionIdIsEmpty_ShouldHaveEmptyPermissionError()
    {
        var command = CreateValidCommand(new List<Guid> { Guid.NewGuid(), Guid.Empty });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PermissionIds)
            .WithErrorMessage(EmptyPermissionMessage);
    }

    [Fact]
    public void Validate_WhenMultiplePermissionIdsAreEmpty_ShouldHaveOneErrorPerEmptyId()
    {
        var command = CreateValidCommand(new List<Guid> { Guid.Empty, Guid.Empty, Guid.NewGuid() });

        var result = _sut.TestValidate(command);

        result.Errors.Count(error => error.ErrorMessage == EmptyPermissionMessage)
            .Should().Be(2, "liste içinde iki boş izin id'si var");
    }

    [Fact]
    public void Validate_WhenOnlyRoleIdIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = CreateValidCommand() with { RoleId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.PermissionIds);
        result.Errors.Should().HaveCount(1, "yalnızca rol id hatası dönmelidir");
    }

    [Fact]
    public void Validate_WhenOnlyPermissionIdsIsInvalid_ShouldNotHaveOtherFieldErrors()
    {
        var command = new UpdateRolePermissionCommand(null!)
        {
            RoleId = Guid.NewGuid()
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
        result.Errors.Should().HaveCount(1, "yalnızca izin listesi hatası dönmelidir");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreInvalid_ShouldHaveTwoValidationErrors()
    {
        // Boş gövde ve route'tan rol id gelmemesi senaryosu: iki alan da boş kalır.
        var command = new UpdateRolePermissionCommand(null!)
        {
            RoleId = Guid.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RoleId).WithErrorMessage(RoleIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.PermissionIds).WithErrorMessage(PermissionIdsRequiredMessage);

        result.Errors.Should().HaveCount(2, "iki alan da geçersiz olduğu için iki hata dönmelidir");
    }

    #endregion
}
