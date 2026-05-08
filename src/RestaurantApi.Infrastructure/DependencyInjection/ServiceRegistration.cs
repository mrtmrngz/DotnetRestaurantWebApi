using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Infrastructure.Cache;

namespace RestaurantApi.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<RedisLockService>();
        services.AddScoped<CacheService>();

        return services;
    }
}