namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class PublicRateLimitSettings
{
    public RateLimitRule Flexible { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}