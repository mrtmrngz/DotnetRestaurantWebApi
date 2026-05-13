using Microsoft.AspNetCore.Identity;

namespace RestaurantApi.Domain.Identity;

public class AppUser: IdentityUser<Guid>
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public bool IsDeleted { get; set; }
    
    public string GetFullName() => $"{Name} {Surname}";
    public bool IsActive() => !IsDeleted;
    public void Deactivate() => IsDeleted = true;
    public void Activate() => IsDeleted = false;
}