namespace RestaurantApi.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductImageUrl { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }

    public void CalculateTotal()
    {
        TotalPrice = UnitPrice * Quantity;
    }
}