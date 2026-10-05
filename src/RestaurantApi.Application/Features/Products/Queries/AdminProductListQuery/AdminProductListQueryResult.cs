using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;

public record AdminProductListQueryResult()
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public decimal Price { get; init; }
    public int CommentCount { get; init; }
    public double AvgRate { get; init; }
    public int TotalSold { get; init; }
    public string CategoryName { get; init; } = null!;
    public string Banner { get; init; } = null!;
    public ProductActiveDiscount? Discount { get; init; }
    public bool IsDeleted { get; init; }
};