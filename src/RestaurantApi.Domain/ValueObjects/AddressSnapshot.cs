namespace RestaurantApi.Domain.ValueObjects;

public class AddressSnapshot
{
    public Guid Id { get; set; }
    public string RecipientName { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Town { get; set; } = null!;
    public string Neighborhood { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string? BuildingInfo { get; set; }
    public string BuildingNumber { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? ZipCode { get; set; }
    public Guid UserId { get; set; }
    public string FullAddress { get; set; } = null!;
}