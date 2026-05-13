using Microsoft.AspNetCore.Identity;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Domain.Identity;

public class AppRole : IdentityRole<Guid>
{
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}