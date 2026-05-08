namespace RestaurantApi.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Quantity { get; set; }

    public Product Product { get; set; } = null!;
    
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
}