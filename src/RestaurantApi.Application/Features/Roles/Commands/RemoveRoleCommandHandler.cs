using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RoleRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Roles.Commands;

public class RemoveRoleCommandHandler: IRequestHandler<RemoveRoleCommand, BaseResponse>
{
    private readonly ILogger<RemoveRoleCommandHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly RoleRules _roleRules;
    private readonly ICacheService _cacheService;

    public RemoveRoleCommandHandler(ILogger<RemoveRoleCommandHandler> logger, IRoleRepository roleRepository,
        RoleRules roleRules, ICacheService cacheService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _roleRules = roleRules;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rol silme işlemi başlıyor. RoleId:{RoleId}", request.RoleId);

        AppRole role = await FindRoleAsync(request.RoleId, cancellationToken);

        _roleRules.ShouldRoleBeDeletable(role, request.RoleId);

        await EnsureRoleHasNoUsersAsync(request.RoleId, cancellationToken);

        int affectedRowCount = await _roleRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.RoleId)
            .ExecuteDeleteAsync(cancellationToken);

        _roleRules.ShouldRoleDeletionBeApplied(affectedRowCount, request.RoleId);

        await InvalidateRolePermissionCacheAsync(request.RoleId, role.Name);

        _logger.LogInformation("Rol silindi. RoleId:{RoleId}, RoleName:{RoleName}", request.RoleId, role.Name);

        return new BaseResponse
        {
            Message = $"'{role.Name}' rolü başarılı bir şekilde silindi.",
            Code = Codes.CONTENT_DELETED_SUCCESS
        };
    }

    #region Helper Methods

    private async Task<AppRole> FindRoleAsync(Guid roleId, CancellationToken ctx)
    {
        _logger.LogInformation("Silinecek rol aranıyor. RoleId:{RoleId}", roleId);

        AppRole? role = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, ctx);

        _roleRules.ShouldRoleExist(role, roleId);

        return role!;
    }

    private async Task EnsureRoleHasNoUsersAsync(Guid roleId, CancellationToken ctx)
    {
        Dictionary<Guid, int> userCounts = await _roleRepository.GetUserCountsByRoleIdAsync(ctx);

        bool roleHasUsers = userCounts.GetValueOrDefault(roleId) > 0;

        _logger.LogInformation("Role bağlı kullanıcı sayısı kontrol edildi. RoleId:{RoleId}, UserCount:{UserCount}",
            roleId, userCounts.GetValueOrDefault(roleId));

        _roleRules.ShouldRoleHaveNoUsers(roleHasUsers, roleId);
    }

    private async Task InvalidateRolePermissionCacheAsync(Guid roleId, string? roleName)
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

    #endregion
}
