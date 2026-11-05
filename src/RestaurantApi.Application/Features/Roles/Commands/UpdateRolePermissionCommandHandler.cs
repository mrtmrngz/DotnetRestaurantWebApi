using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Roles.Commands;

// Rolün izin kümesini istekteki kümeye eşitler: eksik izinler eklenir, istekte olmayanlar silinir.
// İş kuralları (rol var mı, sistem rolü mü, izinler gerçekten var mı) RoleRules üzerinden uygulanır;
// yazma işlemi tek SaveChanges ile atomik yapılır ve rolün izin cache'i temizlenir.
public class UpdateRolePermissionCommandHandler: IRequestHandler<UpdateRolePermissionCommand, BaseResponse>
{
    private readonly ILogger<UpdateRolePermissionCommandHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly RoleRules _roleRules;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRolePermissionCommandHandler(ILogger<UpdateRolePermissionCommandHandler> logger,
        IRoleRepository roleRepository, IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository, RoleRules roleRules, ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleRules = roleRules;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(UpdateRolePermissionCommand request, CancellationToken cancellationToken)
    {
        HashSet<Guid> requestedPermissionIds = request.PermissionIds.ToHashSet();

        _logger.LogInformation(
            "Rol izin güncelleme işlemi başlıyor. RoleId:{RoleId}, RequestedPermissionCount:{RequestedPermissionCount}",
            request.RoleId, requestedPermissionIds.Count);

        // (1) rol var olmalı (404) ve sistem rolü olmamalı (422); rol adı cache temizliği için de gerekir
        AppRole role = await FindUpdatableRoleAsync(request.RoleId, cancellationToken);

        // (2) istekteki izinlerin tamamı var olmalı; biri bile yoksa hiçbir değişiklik yazılmaz (404)
        await EnsurePermissionsExistAsync(requestedPermissionIds, request.RoleId, cancellationToken);

        // (3) mevcut eşleşmelerle istek karşılaştırılır; yalnızca gerçekten değişen satırlar belirlenir
        List<RolePermission> currentAssignments =
            await GetCurrentAssignmentsAsync(request.RoleId, cancellationToken);

        RolePermissionChanges changes =
            CalculateChanges(request.RoleId, requestedPermissionIds, currentAssignments);

        if (!changes.HasChanges)
        {
            _logger.LogInformation(
                "Rol izinlerinde değişiklik yok; güncelleme uygulanmadı. RoleId:{RoleId}, PermissionCount:{PermissionCount}",
                request.RoleId, currentAssignments.Count);

            return BuildUpdatedResponse();
        }

        // (4) ekleme ve silme tek SaveChanges ile atomik yazılır
        await ApplyChangesAsync(changes, request.RoleId, cancellationToken);

        // (5) rol izinleri Redis'te cache'lendiği için temizlenmezse yetki kararı 1 saat boyunca eski kalır
        await InvalidatePermissionCacheAsync(role.Name, request.RoleId);

        _logger.LogInformation(
            "Rol izinleri güncellendi. RoleId:{RoleId}, RoleName:{RoleName}, AddedCount:{AddedCount}, RemovedCount:{RemovedCount}, PermissionCount:{PermissionCount}",
            request.RoleId, role.Name, changes.ToAdd.Count, changes.ToRemove.Count, requestedPermissionIds.Count);

        return BuildUpdatedResponse();
    }

    #region Helper Methods

    // Rolü bulur ve güncellenebilir olduğunu doğrular; kurallar ihlal edilirse 404/422 fırlatır.
    private async Task<AppRole> FindUpdatableRoleAsync(Guid roleId, CancellationToken ctx)
    {
        _logger.LogInformation("Güncellenecek rol aranıyor. RoleId:{RoleId}", roleId);

        AppRole? role = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, ctx);

        _roleRules.ShouldRoleExist(role, roleId);
        _roleRules.ShouldRoleBeUpdatable(role!, roleId);

        return role!;
    }

    // İstekteki izinlerin var olup olmadığı tek sorguda kontrol edilir (boş liste için sorgu atılmaz).
    private async Task EnsurePermissionsExistAsync(IReadOnlyCollection<Guid> requestedPermissionIds, Guid roleId,
        CancellationToken ctx)
    {
        if (requestedPermissionIds.Count == 0)
        {
            return;
        }

        List<Guid> existingPermissionIds = await _permissionRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(permission => requestedPermissionIds.Contains(permission.Id))
            .Select(permission => permission.Id)
            .ToListAsync(ctx);

        _roleRules.ShouldPermissionsExist(requestedPermissionIds, existingPermissionIds, roleId);
    }

    // Rolün mevcut izin eşleşmeleri izlenerek (tracked) getirilir; silinecek satırlar bu listeden çıkar.
    private async Task<List<RolePermission>> GetCurrentAssignmentsAsync(Guid roleId, CancellationToken ctx)
    {
        List<RolePermission> currentAssignments = await _rolePermissionRepository.GetAllAsQueryable()
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .ToListAsync(ctx);

        _logger.LogInformation("Rolün mevcut izinleri getirildi. RoleId:{RoleId}, CurrentPermissionCount:{Count}",
            roleId, currentAssignments.Count);

        return currentAssignments;
    }

    // İstek ile mevcut eşleşmeleri karşılaştırır: istekte olmayanlar silinecek, eksik olanlar eklenecek.
    private static RolePermissionChanges CalculateChanges(Guid roleId, IReadOnlyCollection<Guid> requestedPermissionIds,
        IReadOnlyCollection<RolePermission> currentAssignments)
    {
        HashSet<Guid> currentPermissionIds = currentAssignments
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSet();

        List<RolePermission> toAdd = requestedPermissionIds
            .Except(currentPermissionIds)
            .Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            })
            .ToList();

        List<RolePermission> toRemove = currentAssignments
            .Where(rolePermission => !requestedPermissionIds.Contains(rolePermission.PermissionId))
            .ToList();

        return new RolePermissionChanges(toAdd, toRemove);
    }

    // Ekleme ve silme aynı SaveChanges çağrısında yazılır; değişikliklerin yarısının uygulanması engellenir.
    private async Task ApplyChangesAsync(RolePermissionChanges changes, Guid roleId, CancellationToken ctx)
    {
        if (changes.ToAdd.Count > 0)
        {
            _logger.LogInformation("Role yeni izinler ekleniyor. RoleId:{RoleId}, AddCount:{AddCount}", roleId,
                changes.ToAdd.Count);

            _rolePermissionRepository.AddRange(changes.ToAdd);
        }

        if (changes.ToRemove.Count > 0)
        {
            _logger.LogInformation("Rolden izinler kaldırılıyor. RoleId:{RoleId}, RemoveCount:{RemoveCount}", roleId,
                changes.ToRemove.Count);

            _rolePermissionRepository.RemoveRange(changes.ToRemove);
        }

        await _unitOfWork.SaveChangesAsync(ctx);
    }

    // Yetki kararı Redis'teki "role:{role}:permissions" anahtarından okunur; anahtar silinmezse 1 saat eski kalır.
    private async Task InvalidatePermissionCacheAsync(string? roleName, Guid roleId)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            _logger.LogWarning("Rol adı boş olduğu için izin cache'i temizlenemedi. RoleId:{RoleId}", roleId);
            return;
        }

        string cacheKey = CacheKeys.RolePermissions(roleName.ToLowerInvariant());

        await _cacheService.RemoveAsync(cacheKey);

        _logger.LogInformation("Rol izin cache'i temizlendi. RoleId:{RoleId}, CacheKey:{CacheKey}", roleId, cacheKey);
    }

    private static BaseResponse BuildUpdatedResponse()
    {
        return new BaseResponse
        {
            Message = "Rol izinleri başarılı bir şekilde güncellendi.",
            Code = Codes.CONTENT_UPDATED_SUCCESS
        };
    }

    #endregion

    private readonly record struct RolePermissionChanges(List<RolePermission> ToAdd, List<RolePermission> ToRemove)
    {
        public bool HasChanges => ToAdd.Count > 0 || ToRemove.Count > 0;
    }
}
