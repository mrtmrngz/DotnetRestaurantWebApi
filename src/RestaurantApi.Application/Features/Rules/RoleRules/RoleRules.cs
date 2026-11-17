using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Rules.RoleRules;

// Rol yönetimi akışlarının iş kuralları (rol izinlerinin güncellenmesi ve rol bilgisinin düzenlenmesi).
// Her kural, ilgili durumda log yazıp uygun iş kuralı hatasını fırlatır.
public class RoleRules
{
    private readonly ILogger<RoleRules> _logger;

    public RoleRules(ILogger<RoleRules> logger)
    {
        _logger = logger;
    }

    // (404) Güncellenmek istenen rol yoksa.
    public void ShouldRoleExist(AppRole? role, Guid roleId)
    {
        if (role is not null)
            return;

        _logger.LogWarning("Güncellenmek istenen rol bulunamadı. RoleId:{RoleId}", roleId);
        throw new NotFoundException("Rol bulunamadı.");
    }

    // (422) Sistem rolleri (ADMIN / USER) seed ile yönetilir; izinleri bu uçtan değiştirilemez.
    public void ShouldRoleBeUpdatable(AppRole role, Guid roleId)
    {
        if (!role.IsSystemRole)
            return;

        _logger.LogWarning("Sistem rolünün izinleri güncellenemez. RoleId:{RoleId}, RoleName:{RoleName}",
            roleId, role.Name);

        throw new UnprocessableEntityError("Sistem rollerinin izinleri güncellenemez.");
    }

    // (404) İstekteki izinlerden var olmayan varsa hiçbiri yazılmaz.
    public void ShouldPermissionsExist(IReadOnlyCollection<Guid> requestedPermissionIds,
        IReadOnlyCollection<Guid> existingPermissionIds, Guid roleId)
    {
        // İstekte olup veritabanında bulunmayan izinler: liste boşsa hiçbir şey eksik değildir.
        List<Guid> missingPermissionIds = requestedPermissionIds.Except(existingPermissionIds).ToList();

        if (missingPermissionIds.Count == 0)
            return;

        _logger.LogWarning(
            "İstekteki bazı izinler bulunamadı; rol izinleri güncellenmedi. RoleId:{RoleId}, MissingPermissionCount:{MissingPermissionCount}, MissingPermissionIds:{MissingPermissionIds}",
            roleId, missingPermissionIds.Count, string.Join(", ", missingPermissionIds));

        throw new NotFoundException("İstekteki bazı izinler bulunamadı.");
    }

    // (422) Sistem rollerinin (ADMIN / USER) bilgileri seed ile yönetilir; adı bu uçtan değiştirilemez.
    public void ShouldRoleInfoBeUpdatable(AppRole role, Guid roleId)
    {
        if (!role.IsSystemRole)
            return;

        _logger.LogWarning("Sistem rolünün bilgileri güncellenemez. RoleId:{RoleId}, RoleName:{RoleName}",
            roleId, role.Name);

        throw new UnprocessableEntityError("Sistem rollerinin bilgileri güncellenemez.");
    }

    // (409) Rol adı (ya da normalize edilmiş hali) başka bir role aitse.
    public void ShouldRoleNameBeAvailable(bool anotherRoleHasTheSameName, Guid roleId, string roleName)
    {
        if (!anotherRoleHasTheSameName)
            return;

        _logger.LogWarning("Rol adı başka bir rol tarafından kullanılıyor. RoleId:{RoleId}, RoleName:{RoleName}",
            roleId, roleName);

        throw new ConflictException("Bu rol adı zaten kullanılıyor.");
    }

    // (409) Eşzamanlı bir değişiklik nedeniyle güncelleme uygulanamadıysa (ör. rol bu sırada silindi).
    public void ShouldRoleUpdateBeApplied(int affectedRowCount, Guid roleId)
    {
        if (affectedRowCount > 0)
            return;

        _logger.LogWarning("Rol güncellenemedi; rol bu sırada değişmiş olabilir. RoleId:{RoleId}", roleId);

        throw new ConflictException("Rol güncellenemedi. Lütfen tekrar deneyiniz.");
    }
}
