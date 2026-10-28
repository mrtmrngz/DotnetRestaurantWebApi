using StackExchange.Redis;

namespace RestaurantApi.Infrastructure.Cache;

public class RedisLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string?> AcquireLock(string key, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();

        var token = Guid.NewGuid().ToString("N");

        var acquired = await db.StringSetAsync(
            $"lock:{key}",
            token,
            expiry,
            When.NotExists
        );

        return acquired ? token : null;
    }

    public async Task<bool> ReleaseAsync(string key, string token)
    {
        var db = _redis.GetDatabase();

        return await db.LockReleaseAsync(
            $"lock:{key}",
            token);
    }
}