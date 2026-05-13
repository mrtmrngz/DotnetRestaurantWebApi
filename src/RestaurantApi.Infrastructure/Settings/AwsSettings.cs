namespace RestaurantApi.Infrastructure.Settings;

public class AwsSettings
{
    public string ServiceUrl { get; set; } = null!;
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string Bucket { get; set; } = null!;
    public string PublicBaseUrl { get; set; } = null!;
}