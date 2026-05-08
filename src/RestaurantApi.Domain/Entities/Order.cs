using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public decimal ShippingCost { get; set; }
    public string AddressSnapshot { get; set; } = null!;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime OrderDate { get; set; }


    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    
    public Guid? RestaurantTableId { get; set; }
    public Guid? UserId { get; set; }

    public void CalculateTotals()
    {
        if (!OrderItems.Any())
            throw new Exception("Order cannot be empty");

        SubTotal = OrderItems.Sum(x => x.TotalPrice);
        CalculateShipping();
        Total = SubTotal + ShippingCost;
    }

    private void CalculateShipping()
    {
        ShippingCost = SubTotal >= 500 ? 0 : 120;
    }
}