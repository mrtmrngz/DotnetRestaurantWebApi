namespace RestaurantApi.Domain.Entities;

public class Discount
{
    public Guid Id { get; set; }
    public double Rate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}