using Microsoft.AspNetCore.Identity;

namespace RestaurantApi.Persistence.Identity;

public class AppUser: IdentityUser<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public bool IsDeleted { get; set; }
}