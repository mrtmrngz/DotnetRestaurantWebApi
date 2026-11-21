namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";
    public bool Enabled { get; init; } = true;

    public AuthRateLimitSettings Auth { get; init; } = new();
    public PasswordRateLimitSettings Password { get; init; } = new();
    public SameUserRateLimitSettings SameUser { get; init; } = new();
    public PanelRateLimitSettings Panel { get; init; } = new();
    public CartRateLimitSettings Cart { get; init; } = new();
    public CheckoutRateLimitSettings Checkout { get; init; } = new();
    public CategoryRateLimitSettings Categories { get; init; } = new();
    public PublicRateLimitSettings Public { get; init; } = new();
}