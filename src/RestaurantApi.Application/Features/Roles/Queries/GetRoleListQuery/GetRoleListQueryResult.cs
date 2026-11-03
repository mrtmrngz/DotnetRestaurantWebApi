namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;

public record GetRoleListQueryResult()
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public bool IsSystemRole { get; set; }
}
