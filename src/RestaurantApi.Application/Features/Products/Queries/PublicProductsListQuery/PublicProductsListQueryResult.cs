namespace RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

public record PublicProductsListQueryResult()
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public decimal Price { get; init; }
    public int CommentCount { get; init; }
    public double AvgRate { get; init; }
    public string CategoryName { get; init; } = null!;
    public string Banner { get; init; } = null!;
    public ProductActiveDiscount? Discount { get; set; }
};

public record ProductActiveDiscount()
{
    public Guid Id { get; init; }
    public double DiscountRate { get; init; }
}