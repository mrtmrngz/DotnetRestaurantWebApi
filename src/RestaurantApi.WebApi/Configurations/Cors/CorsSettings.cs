namespace RestaurantApi.WebApi.Configurations.Cors;

public class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "FrontendPolicy";
    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}