using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

namespace RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;

public record PublicProductDetailQueryResult()
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; set; }
    public string Slug { get; init; } = null!;
    public decimal Price { get; init; }
    public int CommentCount { get; init; }
    public double AvgRate { get; init; }
    public string CategoryName { get; init; } = null!;
    public List<ProductDetailImageList> Images { get; init; } = new List<ProductDetailImageList>();
    public ProductActiveDiscount? Discount { get; set; }
};

public record ProductDetailImageList()
{
    public Guid Id { get; set; }
    public string PublicId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}