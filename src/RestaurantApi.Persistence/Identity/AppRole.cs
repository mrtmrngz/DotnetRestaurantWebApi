using Microsoft.AspNetCore.Identity;
using RestaurantApi.Persistence.Entities;

namespace RestaurantApi.Persistence.Identity;

public class AppRole : IdentityRole<Guid>
{
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}