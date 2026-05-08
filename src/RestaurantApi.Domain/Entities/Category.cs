namespace RestaurantApi.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? MediaId { get; set; }
    public Media? Media { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}