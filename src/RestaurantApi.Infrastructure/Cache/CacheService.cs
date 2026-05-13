using System.Text.Json;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Infrastructure.Common;
using StackExchange.Redis;

namespace RestaurantApi.Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisLockService _lockService;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IConnectionMultiplexer redis, RedisLockService lockService, ILogger<CacheService> logger)
    {
        _redis = redis;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<T> GetOrInternalSetAsync<T>(string key, Func<Task<T>> task, TimeSpan? expiration = null)
    {
        var db = _redis.GetDatabase();
        
        _logger.LogInformation("🔍 CACHE CHECK: {Key}", key);

        var cachedData = await db.StringGetAsync(key);

        if (cachedData.HasValue)
        {
            CacheMetrics.Hit();
            _logger.LogInformation("⚡ CACHE HIT: {Key}", key);
            var json = CompressionHelper.Decompress(cachedData!);
            return JsonSerializer.Deserialize<T>(json)!;
        }
        
        CacheMetrics.Miss();
        _logger.LogWarning("❌ CACHE MISS: {Key}", key);

        var lockAcquired = await _lockService.AcquireLock(key, TimeSpan.FromSeconds(5));

        if (!lockAcquired)
        {
            _logger.LogWarning("🔒 LOCK FAILED, retrying: {Key}", key);
            await Task.Delay(50);
            return await GetOrInternalSetAsync(key, task, expiration);
        }

        try
        {
            var result = await task();

            var json = JsonSerializer.Serialize(result);
            var compressed = CompressionHelper.Compress(json);

            await db.StringSetAsync(key, compressed, expiration ?? TimeSpan.FromHours(1));

            _logger.LogInformation("💾 CACHE SET: {Key}", key);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 Redis connection failed for key: {Key}", key);
            return await task();
        }
        finally
        {
            await _lockService.ReleaseAsync(key);
            _logger.LogInformation("🔓 LOCK RELEASED: {Key}", key);
        }
    }

    public async Task SetAsync<T>(string key, T data, TimeSpan? expiration = null)
    {

        int retryCount = 0;
        const int maxRetries = 5;

        while (retryCount < maxRetries)
        {
            var lockAcquired = await _lockService.AcquireLock(key, TimeSpan.FromSeconds(5));

            if (lockAcquired)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var json = JsonSerializer.Serialize(data);
                    var compressed = CompressionHelper.Compress(json);

                    await db.StringSetAsync(key, compressed, expiration ?? TimeSpan.FromHours(1));
                    _logger.LogInformation("💾 CACHE MANUAL SET: {Key}", key);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🔴 Redis SetAsync failed: {Key}", key);
                    return;
                }
                finally
                {
                    await _lockService.ReleaseAsync(key);
                    _logger.LogInformation("🔓 LOCK RELEASED: {Key}", key);
                }
            }

            retryCount++;
            _logger.LogWarning("🔒 LOCK FAILED, retry {Count}/{Max}: {Key}", retryCount, maxRetries, key);
            await Task.Delay(100);
        }

        _logger.LogError("🚫 Could not acquire lock for SetAsync after {Max} retries: {Key}", maxRetries, key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = _redis.GetDatabase();

            var cachedValue = await db.StringGetAsync(key);

            if (cachedValue.HasValue)
            {
                CacheMetrics.Hit();
                _logger.LogInformation("⚡ CACHE HIT: {Key}", key);
                var decompressed = CompressionHelper.Decompress(cachedValue!);
                return JsonSerializer.Deserialize<T>(decompressed)!;
            }
            
            CacheMetrics.Miss();
            _logger.LogWarning("❌ CACHE MISS: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔴 Redis GetAsync failed for key: {Key}", key);
            return default;
        }
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
        _logger.LogInformation("🗑 CACHE REMOVED: {Key}", key);
    }
}