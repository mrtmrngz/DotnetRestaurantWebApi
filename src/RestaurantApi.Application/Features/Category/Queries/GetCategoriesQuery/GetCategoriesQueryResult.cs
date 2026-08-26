namespace RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;

public record GetCategoriesQueryResult
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string ImageUrl { get; init; } = null!;
};