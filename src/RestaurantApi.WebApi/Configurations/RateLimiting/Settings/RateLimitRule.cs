using System.ComponentModel.DataAnnotations;
namespace RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

public sealed class RateLimitRule
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; }

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; init; }
}