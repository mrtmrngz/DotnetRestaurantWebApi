namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class AuthRateLimitSettings
{
    public RateLimitRule Register { get; init; } = new() { PermitLimit = 3, WindowSeconds = 600 };
    public RateLimitRule General { get; init; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    public RateLimitRule Refresh { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitRule MailVerify { get; init; } = new() { PermitLimit = 5, WindowSeconds = 300 };
}