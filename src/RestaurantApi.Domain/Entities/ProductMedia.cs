namespace RestaurantApi.Domain.Entities;

public class ProductMedia
{
    public Guid Id { get; set; }
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid MediaId { get; set; }
    public Media Media { get; set; } = null!;
}