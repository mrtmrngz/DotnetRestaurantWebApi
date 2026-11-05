namespace RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;

public record GetRolePermissionsQueryResult()
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
