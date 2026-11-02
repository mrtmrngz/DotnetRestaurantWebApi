using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;

public record GetUserOrderDetailQueryResult()
{
    public Guid OrderId { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public string? AddressSnapshot { get; set; }

    public List<OrderItemDetail> OrderItems { get; set; } = new List<OrderItemDetail>();
}

public record OrderItemDetail()
{
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
