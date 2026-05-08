namespace RestaurantApi.Infrastructure.Cache;

public static class CacheMetrics
{
    private static int _hit;
    private static int _miss;

    public static void Hit() => _hit++;
    public static void Miss() => _miss++;

    public static object GetStats()
    {
        var total = _hit + _miss;
        var ratio = total == 0 ? 0 : (double)_hit / total * 100;

        return new
        {
            Hit = _hit,
            Miss = _miss,
            HitRatio = $"{ratio:0.00}%"
        };
    }
}