namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;

public record GetRestaurantTableListQueryResult
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public int GuestCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
