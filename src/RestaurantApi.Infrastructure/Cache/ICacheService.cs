namespace RestaurantApi.Infrastructure.Cache;

public interface ICacheService
{
    Task<T> GetOrInternalSetAsync<T>(
        string key,
        Func<Task<T>> task,
        TimeSpan? expiration = null
    );

    Task RemoveAsync(string key);
}