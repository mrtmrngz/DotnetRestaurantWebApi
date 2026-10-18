namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;

public record GetAvailableTablesQueryResult
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public int Capacity { get; init; }
}
