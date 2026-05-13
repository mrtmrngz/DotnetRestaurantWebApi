using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Infrastructure.Security.Authorization.Requirements;

namespace RestaurantApi.Infrastructure.Security.Authorization.Handler;

public class RedisPermissionHandler: AuthorizationHandler<PermissionRequirement>
{

    private readonly ICacheService _cacheService;
    private readonly IPermissionRepository _permissionRepository;

    public RedisPermissionHandler(ICacheService cacheService, IPermissionRepository permissionRepository)
    {
        _cacheService = cacheService;
        _permissionRepository = permissionRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            return;
        }

        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        foreach (var role in roles)
        {
            var cacheKey = CacheKeys.RolePermissions(role.ToLowerInvariant());

            var permissions = await _cacheService.GetOrInternalSetAsync(
                cacheKey,
                async () => await _permissionRepository.GetPermissionsByRoleNameAsync(role),
                TimeSpan.FromHours(1)
            );

            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}