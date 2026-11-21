namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class CategoryRateLimitSettings
{
    public RateLimitRule GetCategories { get; init; } = new() { PermitLimit = 20, WindowSeconds = 60 };
}