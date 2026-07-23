namespace RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;

public record GetUserAddressQueryResult(
    Guid Id,
    string Title,
    string RecipientName,
    string City,
    string Town,
    string Neighborhood,
    string Street,
    string? BuildingInfo = null,
    string BuildingNumber = default!,
    string PhoneNumber = default!,
    bool IsDefault = default!,
    string ZipCode = default!);