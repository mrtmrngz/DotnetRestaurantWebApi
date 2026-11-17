namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;

// Rol bilgisi detayı: rolün kimliği, adı, normalize edilmiş adı ve sistem rolü olup olmadığı döner.
public record GetRoleInfoQueryResult()
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public bool IsSystemRole { get; set; }
}
