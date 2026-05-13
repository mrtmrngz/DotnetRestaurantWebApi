using Microsoft.AspNetCore.Authorization;

namespace RestaurantApi.Infrastructure.Security.Authorization.Requirements;

public class PermissionRequirement: IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}