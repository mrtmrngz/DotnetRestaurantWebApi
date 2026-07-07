using RestaurantApi.Domain.Identity;

namespace RestaurantApi.IntegrationTests.Setup;

public class VanillaUserSetupDto
{
    public string AccessToken { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}