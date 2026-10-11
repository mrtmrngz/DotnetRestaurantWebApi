using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;

public record GetDiscountListQueryResult
{
    public Guid Id { get; init; }
    public double DiscountRate { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DiscountStatus Status { get; set; }
}
