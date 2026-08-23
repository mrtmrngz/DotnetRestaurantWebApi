namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface ISlugService
{
    string ToSlug(string text);

    Task<string> GenerateUniqueSlugAsync<TEntity>(
        IQueryable<TEntity> query,
        string title,
        CancellationToken ctx = default
    ) where TEntity : class;
}