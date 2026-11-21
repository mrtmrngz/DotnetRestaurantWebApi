namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class CartRateLimitSettings
{
    public RateLimitRule GeneralCart { get; init; } = new() { PermitLimit = 50, WindowSeconds = 60 };
    public RateLimitRule UserRemoveCart { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}