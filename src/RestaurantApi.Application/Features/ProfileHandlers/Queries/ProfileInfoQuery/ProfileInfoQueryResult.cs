namespace RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;

public record ProfileInfoQueryResult(
    Guid Id = default,
    string Name = "",
    string Surname = "",
    string Email = "",
    bool TwoFactorStatus = false,
    string PhoneNumber = ""
);