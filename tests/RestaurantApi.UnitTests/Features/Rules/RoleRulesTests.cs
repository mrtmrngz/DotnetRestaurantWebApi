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
}
