namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class SameUserRateLimitSettings
{
    public RateLimitRule Standard { get; init; } = new() { PermitLimit = 15, WindowSeconds = 60 };
    public RateLimitRule Strict { get; init; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitRule Restricted { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitRule Critical { get; init; } = new() { PermitLimit = 3, WindowSeconds = 600 };
    public RateLimitRule Flexible { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}