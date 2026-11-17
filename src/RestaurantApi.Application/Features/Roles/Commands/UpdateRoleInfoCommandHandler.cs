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

public class UpdateRoleInfoCommandHandler: IRequestHandler<UpdateRoleInfoCommand, BaseResponse>
{
    private readonly ILogger<UpdateRoleInfoCommandHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly RoleRules _roleRules;
    private readonly ICacheService _cacheService;

    public UpdateRoleInfoCommandHandler(ILogger<UpdateRoleInfoCommandHandler> logger, IRoleRepository roleRepository,
        IUserRepository userRepository, RoleRules roleRules, ICacheService cacheService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _roleRules = roleRules;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(UpdateRoleInfoCommand request, CancellationToken cancellationToken)
    {
        string requestedRoleName = request.Name.Trim();
        string requestedNormalizedName = requestedRoleName.ToUpperInvariant();

        _logger.LogInformation("Rol bilgisi güncelleme işlemi başlıyor. RoleId:{RoleId}, NewRoleName:{NewRoleName}",
            request.RoleId, requestedRoleName);

        AppRole role = await FindUpdatableRoleAsync(request.RoleId, cancellationToken);

        if (role.Name == requestedRoleName && role.NormalizedName == requestedNormalizedName)
        {
            _logger.LogInformation(
                "Rol adı istekteki adla aynı; güncelleme uygulanmadı. RoleId:{RoleId}, RoleName:{RoleName}",
                request.RoleId, role.Name);

            return BuildUpdatedResponse(requestedRoleName);
        }

        await EnsureRoleNameIsAvailableAsync(request.RoleId, requestedRoleName, requestedNormalizedName,
            cancellationToken);

        IList<AppUser> roleUsers = await _userRepository.GetUsersInRoleAsync(role.Name!);

        _logger.LogInformation("Rol adı değişmeden önce role bağlı kullanıcılar alındı. RoleId:{RoleId}, UserCount:{UserCount}",
            request.RoleId, roleUsers.Count);

        int affectedRowCount = await _roleRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.RoleId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, requestedRoleName)
                .SetProperty(x => x.NormalizedName, requestedNormalizedName), cancellationToken);

        _roleRules.ShouldRoleUpdateBeApplied(affectedRowCount, request.RoleId);

        await InvalidateRoleCachesAsync(request.RoleId, role.Name, requestedRoleName, roleUsers);

        _logger.LogInformation(
            "Rol bilgisi güncellendi. RoleId:{RoleId}, OldRoleName:{OldRoleName}, NewRoleName:{NewRoleName}",
            request.RoleId, role.Name, requestedRoleName);

        return BuildUpdatedResponse(requestedRoleName);
    }

    #region Helper Methods

    private async Task<AppRole> FindUpdatableRoleAsync(Guid roleId, CancellationToken ctx)
    {
        _logger.LogInformation("Güncellenecek rol aranıyor. RoleId:{RoleId}", roleId);

        AppRole? role = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == roleId, ctx);

        _roleRules.ShouldRoleExist(role, roleId);
        _roleRules.ShouldRoleInfoBeUpdatable(role!, roleId);

        return role!;
    }

    private async Task EnsureRoleNameIsAvailableAsync(Guid roleId, string requestedRoleName,
        string requestedNormalizedName, CancellationToken ctx)
    {
        bool anotherRoleHasTheSameName = await _roleRepository.GetAllAsQueryable()
            .AsNoTracking()
            .AnyAsync(x => x.Id != roleId
                           && (x.Name == requestedRoleName || x.NormalizedName == requestedNormalizedName), ctx);

        _roleRules.ShouldRoleNameBeAvailable(anotherRoleHasTheSameName, roleId, requestedRoleName);
    }

    private async Task InvalidateRoleCachesAsync(Guid roleId, string? oldRoleName, string newRoleName,
        IList<AppUser> roleUsers)
    {
        if (!string.IsNullOrWhiteSpace(oldRoleName))
        {
            string oldPermissionCacheKey = CacheKeys.RolePermissions(oldRoleName.ToLowerInvariant());

            await _cacheService.RemoveAsync(oldPermissionCacheKey);

            _logger.LogInformation("Eski rol adının izin cache'i temizlendi. RoleId:{RoleId}, CacheKey:{CacheKey}",
                roleId, oldPermissionCacheKey);
        }

        string newPermissionCacheKey = CacheKeys.RolePermissions(newRoleName.ToLowerInvariant());

        await _cacheService.RemoveAsync(newPermissionCacheKey);

        foreach (AppUser user in roleUsers)
        {
            await _cacheService.RemoveAsync(CacheKeys.UserRoles(user.Id.ToString()));
        }

        _logger.LogInformation(
            "Rol adına bağlı cache anahtarları temizlendi. RoleId:{RoleId}, CacheKey:{CacheKey}, UserCount:{UserCount}",
            roleId, newPermissionCacheKey, roleUsers.Count);
    }

    private static BaseResponse BuildUpdatedResponse(string roleName)
    {
        return new BaseResponse
        {
            Message = $"'{roleName}' rolünün bilgileri başarılı bir şekilde güncellendi.",
            Code = Codes.CONTENT_UPDATED_SUCCESS
        };
    }

    #endregion
}
