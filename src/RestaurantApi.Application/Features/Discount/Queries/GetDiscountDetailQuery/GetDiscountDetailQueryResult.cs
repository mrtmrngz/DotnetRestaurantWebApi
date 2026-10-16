using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;

public record GetDiscountDetailQueryResult
{
    public Guid Id { get; init; }
    public double DiscountRate { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DiscountStatus Status { get; set; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;

    public decimal? ProductPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
}
