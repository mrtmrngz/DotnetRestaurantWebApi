namespace RestaurantApi.Application.Models.Dtos.AddressDtos;

public sealed record CreateAddressDto(
    Guid UserId,
    string Title,
    string RecipientName,
    string City,
    string Town,
    string Neighborhood,
    string Street,
    string? BuildingInfo,
    string BuildingNumber,
    string PhoneNumber,
    bool IsDefault,
    string? ZipCode
);