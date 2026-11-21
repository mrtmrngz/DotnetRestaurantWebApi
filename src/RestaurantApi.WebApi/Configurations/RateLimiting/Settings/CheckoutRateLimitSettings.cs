namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class CheckoutRateLimitSettings
{
    public RateLimitRule User { get; init; } = new() { PermitLimit = 5, WindowSeconds = 900 };
    public RateLimitRule HybridPanel { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}