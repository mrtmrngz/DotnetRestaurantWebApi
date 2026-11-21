namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class PasswordRateLimitSettings
{
    public RateLimitRule ChangeVerify { get; init; } = new() { PermitLimit = 5, WindowSeconds = 600 };
    public RateLimitRule ChangeApply { get; init; } = new() { PermitLimit = 1, WindowSeconds = 600 };
}