using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

public record GetUserOrdersQueryResult()
{
    public Guid OrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public DateTime OrderDate { get; set; }
}
