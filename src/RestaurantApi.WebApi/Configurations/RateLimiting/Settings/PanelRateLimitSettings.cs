namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class PanelRateLimitSettings
{
    public RateLimitRule GeneralRead { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    public RateLimitRule Put { get; init; } = new() { PermitLimit = 10, WindowSeconds = 300 };
    public RateLimitRule GeneralPost { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitRule GeneralPatch { get; init; } = new() { PermitLimit = 15, WindowSeconds = 300 };
    public RateLimitRule GeneralDelete { get; init; } = new() { PermitLimit = 5, WindowSeconds = 300 };
    
    public RateLimitRule StrictPatch { get; init; } = new() { PermitLimit = 15, WindowSeconds = 60 };
    public RateLimitRule FlexiblePatch { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    
    public RateLimitRule Order { get; init; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}