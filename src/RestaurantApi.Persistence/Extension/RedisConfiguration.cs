using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace RestaurantApi.Persistence.Extension;

public static class RedisConfiguration
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetValue<string>("ConnectionStrings:Redis");

        if (string.IsNullOrEmpty(redisConnectionString))
            throw new ArgumentNullException(nameof(redisConnectionString), "Redis bağlantı cümlesi bulunamadı!");

        var options = ConfigurationOptions.Parse(redisConnectionString);

        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        return services;
    }
}