namespace RestaurantApi.Domain.Entities;

public class Address
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Town { get; set; } = null!;
    public string Neighborhood { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string? BuildingInfo { get; set; }
    public string BuildingNumber { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public bool IsDefault { get; set; }
    public string? ZipCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public Guid UserId { get; set; }
    
    public string FullAddress => string.Join(" ", new[]
    {
        $"{Neighborhood} Mah. {Street} Sok.",
        !string.IsNullOrWhiteSpace(BuildingInfo) ? $"Bina:{BuildingInfo}" : null,
        $"Apartman/Bina No:{BuildingNumber}",
        $"{Town}/{City}",
        ZipCode
    }.Where(s => !string.IsNullOrWhiteSpace(s)));
}