using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.UnitTests.Features.Rules;

// Rol izin güncelleme akışının iş kuralları: rol bulunmalı (404), sistem rolü olmamalı (422)
// ve istekteki izinlerin tamamı var olmalı (404).
public class RoleRulesTests
{
    private readonly ILogger<RoleRules> _loggerMock = Substitute.For<ILogger<RoleRules>>();
    private readonly RoleRules _sut;

    public RoleRulesTests()
    {
        _sut = new RoleRules(_loggerMock);
    }

    #region Helper Methods

    private static AppRole CreateRole(string name = "EDITOR", bool isSystemRole = false)
    {
        return new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsSystemRole = isSystemRole
        };
    }

    #endregion

    #region ShouldRoleExist Tests

    [Fact]
    public void ShouldRoleExist_WhenRoleNotExist_ShouldThrowNotFoundException()
    {
        Action act = () => _sut.ShouldRoleExist(null, Guid.NewGuid());

        act.Should().Throw<NotFoundException>("Rol bulunamadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Rol bulunamadı.");
    }

    [Fact]
    public void ShouldRoleExist_WhenRoleExist_ShouldNotThrow()
    {
        var role = CreateRole();

        Action act = () => _sut.ShouldRoleExist(role, role.Id);

        act.Should().NotThrow("Rol bulunduğu için bu hata fırlatmaması gerekirdi");
    }

    #endregion

    #region ShouldRoleBeUpdatable Tests

    [Fact]
    public void ShouldRoleBeUpdatable_WhenRoleIsSystemRole_ShouldThrowUnprocessableEntityError()
    {
        var role = CreateRole(AppRoles.Admin, isSystemRole: true);

        Action act = () => _sut.ShouldRoleBeUpdatable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>(
                "Sistem rolünün izinleri güncellenemeyeceği için bu hata fırlatılması gerekirdi")
            .WithMessage("Sistem rollerinin izinleri güncellenemez.");
    }

    [Fact]
    public void ShouldRoleBeUpdatable_WhenRoleIsNotSystemRole_ShouldNotThrow()
    {
        var role = CreateRole("EDITOR", isSystemRole: false);

        Action act = () => _sut.ShouldRoleBeUpdatable(role, role.Id);

        act.Should().NotThrow("Uygulamada oluşturulan roller güncellenebildiği için hata fırlatılmamalıydı");
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.User)]
    public void ShouldRoleBeUpdatable_WhenRoleNameIsASystemRoleName_ShouldThrow(string roleName)
    {
        var role = CreateRole(roleName, isSystemRole: true);

        Action act = () => _sut.ShouldRoleBeUpdatable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>(
            $"{roleName} sistem rolü olduğu için güncellenemez.");
    }

    #endregion

    #region ShouldPermissionsExist Tests

    [Fact]
    public void ShouldPermissionsExist_WhenAllRequestedPermissionsExist_ShouldNotThrow()
    {
        var permissionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        Action act = () => _sut.ShouldPermissionsExist(permissionIds, permissionIds, Guid.NewGuid());

        act.Should().NotThrow("İstekteki izinlerin tamamı var olduğu için hata fırlatılmamalıydı");
    }

    [Fact]
    public void ShouldPermissionsExist_WhenSomePermissionsAreMissing_ShouldThrowNotFoundException()
    {
        var existingPermissionId = Guid.NewGuid();
        var missingPermissionId = Guid.NewGuid();

        var requestedPermissionIds = new List<Guid> { existingPermissionId, missingPermissionId };
        var existingPermissionIds = new List<Guid> { existingPermissionId };

        Action act = () => _sut.ShouldPermissionsExist(requestedPermissionIds, existingPermissionIds, Guid.NewGuid());

        act.Should().Throw<NotFoundException>(
                "İstekte var olmayan izin bulunduğu için bu hata fırlatılması gerekirdi")
            .WithMessage("İstekteki bazı izinler bulunamadı.");
    }

    [Fact]
    public void ShouldPermissionsExist_WhenNoPermissionIsRequested_ShouldNotThrow()
    {
        var existingPermissionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        Action act = () => _sut.ShouldPermissionsExist(Array.Empty<Guid>(), existingPermissionIds, Guid.NewGuid());

        act.Should().NotThrow("Boş liste rolün tüm izinlerini kaldırmak demektir, eksik izin yoktur");
    }

    [Fact]
    public void ShouldPermissionsExist_WhenRequestedPermissionsAreASubset_ShouldNotThrow()
    {
        // Sorgu istekteki id'lere göre filtrelendiği için beklenen durum budur; kural bunu hata saymamalıdır.
        var sharedPermissionId = Guid.NewGuid();

        var requestedPermissionIds = new List<Guid> { sharedPermissionId };
        var existingPermissionIds = new List<Guid> { sharedPermissionId };

        Action act = () => _sut.ShouldPermissionsExist(requestedPermissionIds, existingPermissionIds, Guid.NewGuid());

        act.Should().NotThrow("İstenen izinlerin tamamı bulunduğu için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRoleInfoBeUpdatable Tests

    [Fact]
    public void ShouldRoleInfoBeUpdatable_WhenRoleIsSystemRole_ShouldThrowUnprocessableEntityError()
    {
        var role = CreateRole(AppRoles.Admin, isSystemRole: true);

        Action act = () => _sut.ShouldRoleInfoBeUpdatable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>(
                "Sistem rolünün bilgileri güncellenemeyeceği için bu hata fırlatılması gerekirdi")
            .WithMessage("Sistem rollerinin bilgileri güncellenemez.");
    }

    [Fact]
    public void ShouldRoleInfoBeUpdatable_WhenRoleIsNotSystemRole_ShouldNotThrow()
    {
        var role = CreateRole("EDITOR", isSystemRole: false);

        Action act = () => _sut.ShouldRoleInfoBeUpdatable(role, role.Id);

        act.Should().NotThrow("Uygulamada oluşturulan roller güncellenebildiği için hata fırlatılmamalıydı");
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.User)]
    public void ShouldRoleInfoBeUpdatable_WhenRoleNameIsASystemRoleName_ShouldThrow(string roleName)
    {
        var role = CreateRole(roleName, isSystemRole: true);

        Action act = () => _sut.ShouldRoleInfoBeUpdatable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>($"{roleName} sistem rolü olduğu için güncellenemez.");
    }

    #endregion

    #region ShouldRoleNameBeAvailable Tests

    [Fact]
    public void ShouldRoleNameBeAvailable_WhenAnotherRoleHasTheSameName_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldRoleNameBeAvailable(true, Guid.NewGuid(), "KITCHEN");

        act.Should().Throw<ConflictException>(
                "Rol adı başka bir rol tarafından kullanıldığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Bu rol adı zaten kullanılıyor.");
    }

    [Fact]
    public void ShouldRoleNameBeAvailable_WhenNoOtherRoleHasTheName_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldRoleNameBeAvailable(false, Guid.NewGuid(), "KITCHEN");

        act.Should().NotThrow("Rol adı kullanılabilir olduğu için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRoleUpdateBeApplied Tests

    [Fact]
    public void ShouldRoleUpdateBeApplied_WhenNoRowIsAffected_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldRoleUpdateBeApplied(affectedRowCount: 0, Guid.NewGuid());

        act.Should().Throw<ConflictException>(
                "Güncelleme uygulanamadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Rol güncellenemedi. Lütfen tekrar deneyiniz.");
    }

    [Fact]
    public void ShouldRoleUpdateBeApplied_WhenRowIsAffected_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldRoleUpdateBeApplied(affectedRowCount: 1, Guid.NewGuid());

        act.Should().NotThrow("Güncelleme uygulandığı için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRoleNameBeAvailable Tests (rol oluşturma akışı)

    [Fact]
    public void ShouldRoleNameBeAvailable_WhenRoleNameIsTakenAndNoRoleIdIsGiven_ShouldThrowConflictException()
    {
        // Oluşturma akışında güncellenen bir rol olmadığı için rol id verilmez.
        Action act = () => _sut.ShouldRoleNameBeAvailable(true, roleId: null, "KITCHEN");

        act.Should().Throw<ConflictException>(
                "Yeni rolün adı başka bir rol tarafından kullanıldığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Bu rol adı zaten kullanılıyor.");
    }

    [Fact]
    public void ShouldRoleNameBeAvailable_WhenRoleNameIsFreeAndNoRoleIdIsGiven_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldRoleNameBeAvailable(false, roleId: null, "KITCHEN");

        act.Should().NotThrow("Rol adı kullanılabilir olduğu için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRoleBeDeletable Tests

    [Fact]
    public void ShouldRoleBeDeletable_WhenRoleIsSystemRole_ShouldThrowUnprocessableEntityError()
    {
        var role = CreateRole(AppRoles.Admin, isSystemRole: true);

        Action act = () => _sut.ShouldRoleBeDeletable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>(
                "Sistem rolü silinemeyeceği için bu hata fırlatılması gerekirdi")
            .WithMessage("Sistem rolleri silinemez.");
    }

    [Fact]
    public void ShouldRoleBeDeletable_WhenRoleIsNotSystemRole_ShouldNotThrow()
    {
        var role = CreateRole("EDITOR", isSystemRole: false);

        Action act = () => _sut.ShouldRoleBeDeletable(role, role.Id);

        act.Should().NotThrow("Uygulama rolleri silinebildiği için hata fırlatılmamalıydı");
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.User)]
    public void ShouldRoleBeDeletable_WhenRoleNameIsASystemRoleName_ShouldThrow(string roleName)
    {
        var role = CreateRole(roleName, isSystemRole: true);

        Action act = () => _sut.ShouldRoleBeDeletable(role, role.Id);

        act.Should().Throw<UnprocessableEntityError>($"{roleName} sistem rolü olduğu için silinemez.");
    }

    #endregion

    #region ShouldRoleHaveNoUsers Tests

    [Fact]
    public void ShouldRoleHaveNoUsers_WhenRoleHasUsers_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldRoleHaveNoUsers(true, Guid.NewGuid());

        act.Should().Throw<ConflictException>(
                "Role bağlı kullanıcılar olduğu için bu hata fırlatılması gerekirdi")
            .WithMessage("Role bağlı kullanıcılar bulunduğu için rol silinemez.");
    }

    [Fact]
    public void ShouldRoleHaveNoUsers_WhenRoleHasNoUsers_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldRoleHaveNoUsers(false, Guid.NewGuid());

        act.Should().NotThrow("Role bağlı kullanıcı olmadığı için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRoleDeletionBeApplied Tests

    [Fact]
    public void ShouldRoleDeletionBeApplied_WhenNoRowIsAffected_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldRoleDeletionBeApplied(affectedRowCount: 0, Guid.NewGuid());

        act.Should().Throw<ConflictException>(
                "Silme uygulanamadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Rol silinemedi. Lütfen tekrar deneyiniz.");
    }

    [Fact]
    public void ShouldRoleDeletionBeApplied_WhenRowIsAffected_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldRoleDeletionBeApplied(affectedRowCount: 1, Guid.NewGuid());

        act.Should().NotThrow("Silme uygulandığı için hata fırlatılmamalıydı");
    }

    #endregion

    #region ShouldRolesExist Tests

    [Fact]
    public void ShouldRolesExist_WhenAllRequestedRolesExist_ShouldNotThrow()
    {
        var roleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        Action act = () => _sut.ShouldRolesExist(roleIds, roleIds.ToHashSet());

        act.Should().NotThrow("İstekteki rollerin tamamı var olduğu için hata fırlatılmamalıydı");
    }

    [Fact]
    public void ShouldRolesExist_WhenSomeRolesAreMissing_ShouldThrowNotFoundException()
    {
        var existingRoleId = Guid.NewGuid();
        var missingRoleId = Guid.NewGuid();

        IReadOnlyCollection<Guid> dbIds = new List<Guid> { existingRoleId };
        HashSet<Guid> requestedIds = new HashSet<Guid> { existingRoleId, missingRoleId };

        Action act = () => _sut.ShouldRolesExist(dbIds, requestedIds);

        act.Should().Throw<NotFoundException>(
                "İstekte var olmayan rol bulunduğu için bu hata fırlatılması gerekirdi")
            .WithMessage("İstekteki bazı roller bulunamadı.");
    }

    [Fact]
    public void ShouldRolesExist_WhenNoRoleIsRequested_ShouldNotThrow()
    {
        IReadOnlyCollection<Guid> dbIds = new List<Guid> { Guid.NewGuid() };

        Action act = () => _sut.ShouldRolesExist(dbIds, new HashSet<Guid>());

        act.Should().NotThrow("Boş liste eksik rol içermediği için hata fırlatılmamalıydı");
    }

    #endregion
}
