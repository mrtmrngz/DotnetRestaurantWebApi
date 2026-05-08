using StackExchange.Redis;

namespace RestaurantApi.Infrastructure.Cache;

public class RedisLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> AcquireLock(string key, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        return await db.StringSetAsync($"lock:{key}", "1", expiry, When.NotExists);
    }

    public async Task ReleaseAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"lock:{key}");
    }
}