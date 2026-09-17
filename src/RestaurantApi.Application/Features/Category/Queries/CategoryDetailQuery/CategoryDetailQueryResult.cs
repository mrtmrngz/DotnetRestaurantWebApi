namespace RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;

public record CategoryDetailQueryResult()
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string ImageUrl { get; init; } = null!;
    public bool IsDeleted { get; init; }
};