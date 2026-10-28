namespace RestaurantApi.Application.Common.Abstractions;

public interface ICacheService
{
    Task<T> GetOrInternalSetAsync<T>(
        string key,
        Func<Task<T>> task,
        TimeSpan? expiration = null
    );
    Task<bool> TrySetAsync<T>(
        string key,
        T value,
        TimeSpan expiration);
    Task RemoveAsync(string key);
    Task SetAsync<T>(string key, T data, TimeSpan? expiration);
    Task<T?> GetAsync<T>(string key);
}