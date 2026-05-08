namespace RestaurantApi.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public Guid? RestaurantTableId { get; set; }
    public RestaurantTable? RestaurantTable { get; set; }

    public Guid? UserId { get; set; }
}