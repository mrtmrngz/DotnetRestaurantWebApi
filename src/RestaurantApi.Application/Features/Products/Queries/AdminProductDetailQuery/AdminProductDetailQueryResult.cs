using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;

public record AdminProductDetailQueryResult()
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
    public bool IsDeleted { get; init; }
    public int TotalSold { get; init; }
};