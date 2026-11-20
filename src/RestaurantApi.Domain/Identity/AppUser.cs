using Microsoft.AspNetCore.Identity;

namespace RestaurantApi.Domain.Identity;

public class AppUser: IdentityUser<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool IsSystemUser { get; set; } = false;
    
    public string GetFullName() => $"{Name} {Surname}";
}