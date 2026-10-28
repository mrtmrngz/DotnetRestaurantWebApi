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

    public CacheService(
        IConnectionMultiplexer redis,
        RedisLockService lockService,
        ILogger<CacheService> logger)
    {
        _redis = redis;
        _lockService = lockService;
        _logger = logger;
    }

    public async Task<T> GetOrInternalSetAsync<T>(
        string key,
        Func<Task<T>> task,
        TimeSpan? expiration = null)
    {
        IDatabase db;

        try
        {
            db = _redis.GetDatabase();

            var (found, value) = await TryReadCacheAsync<T>(db, key);
            if (found)
                return value!;


            CacheMetrics.Miss();

            _logger.LogInformation("CACHE MISS: {Key}", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Redis erişilemiyor. Veri kaynağından okunuyor: {Key}",
                key);

            return await task();
        }

        string? lockToken = null;

        try
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                lockToken = await _lockService.AcquireLock(
                    key,
                    TimeSpan.FromSeconds(30));

                if (lockToken is not null)
                    break;

                await Task.Delay(100);

                var (waitFound, waitValue) = await TryReadCacheAsync<T>(db, key);
                if (waitFound)
                    return waitValue!;
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Redis lock işlemi başarısız. Veri kaynağından okunuyor: {Key}",
                key);

            return await task();
        }

        if (lockToken is null)
        {
            _logger.LogWarning(
                "Cache lock alınamadı. Veri kaynağından okunuyor: {Key}",
                key);

            return await task();
        }

        try
        {
            // Lock alındıktan sonra cache tekrar kontrol edilir.
            try
            {
                var (recheckFound, recheckValue) = await TryReadCacheAsync<T>(db, key);
                if (recheckFound)
                    return recheckValue!;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cache yeniden okunamadı: {Key}",
                    key);
            }

            var result = await task();

            try
            {
                var json = JsonSerializer.Serialize(result);
                var compressed = CompressionHelper.Compress(json);

                await db.StringSetAsync(
                    key,
                    compressed,
                    expiration ?? TimeSpan.FromHours(1));

                _logger.LogInformation("CACHE SET: {Key}", key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis'e yazılamadı. Veri kaynağının sonucu kullanılacak: {Key}",
                    key);
            }

            return result;
        }
        finally
        {
            try
            {
                await _lockService.ReleaseAsync(key, lockToken);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Cache lock serbest bırakılamadı: {Key}",
                    key);
            }
        }
    }

    public async Task<bool> TrySetAsync<T>(
        string key,
        T value,
        TimeSpan expiration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            expiration,
            TimeSpan.Zero);

        var db = _redis.GetDatabase();

        var json = JsonSerializer.Serialize(value);
        var compressed = CompressionHelper.Compress(json);

        var created = await db.StringSetAsync(
            key,
            compressed,
            expiration,
            When.NotExists);

        _logger.LogInformation(
            "CACHE TRY SET: {Key}, Created: {Created}",
            key,
            created);

        return created;
    }

    public async Task SetAsync<T>(
        string key,
        T data,
        TimeSpan? expiration = null)
    {
        try
        {
            var db = _redis.GetDatabase();

            var json = JsonSerializer.Serialize(data);
            var compressed = CompressionHelper.Compress(json);

            await db.StringSetAsync(
                key,
                compressed,
                expiration ?? TimeSpan.FromHours(1));

            _logger.LogInformation("CACHE SET: {Key}", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Redis cache yazma işlemi başarısız: {Key}",
                key);
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var db = _redis.GetDatabase();

            var (found, value) = await TryReadCacheAsync<T>(db, key);
            if (found)
                return value;

            CacheMetrics.Miss();

            _logger.LogInformation("CACHE MISS: {Key}", key);

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Cache okuma işlemi başarısız: {Key}",
                key);

            return default;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();

            await db.KeyDeleteAsync(key);

            _logger.LogInformation("CACHE REMOVED: {Key}", key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Cache silme işlemi başarısız: {Key}",
                key);
        }
    }

    private async Task<(bool Found, T? Value)> TryReadCacheAsync<T>(IDatabase db, string key)
    {
        var cached = await db.StringGetAsync(key);

        if (!cached.HasValue)
            return (false, default);

        try
        {
            var json = CompressionHelper.Decompress(cached!);
            var value = JsonSerializer.Deserialize<T>(json);

            CacheMetrics.Hit();
            _logger.LogDebug("CACHE HIT: {Key}", key);

            return (true, value);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            _logger.LogError(ex, "Corrupt cache entry, evicting: {Key}", key);
            await db.KeyDeleteAsync(key);

            return (false, default);
        }
    }
}