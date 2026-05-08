namespace RestaurantApi.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CommentCount { get; set; }
    public double AvgRate { get; set; }
    public int TotalSold { get; set; }
    public bool IsDeleted { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<ProductMedia> ProductMedias { get; set; } = new List<ProductMedia>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public Discount? ActiveDiscount => Discounts.FirstOrDefault(x => x.IsActive);
}