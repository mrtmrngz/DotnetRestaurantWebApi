namespace RestaurantApi.Infrastructure.Cache;

public static class CacheMetrics
{
    private static int _hit;
    private static int _miss;

    public static void Hit() => _hit++;
    public static void Miss() => _miss++;

    public static CacheMetricsStats GetStats()
    {
        var total = _hit + _miss;
        var ratio = total == 0 ? 0 : (double)_hit / total * 100;

        return new CacheMetricsStats(_hit, _miss, $"{ratio:0.00}%");
    }
}

// Cache hit/miss sayaçları; HitRatio yüzde metni olarak (ör. "85.00%") döner.
public record CacheMetricsStats(int Hit, int Miss, string HitRatio);