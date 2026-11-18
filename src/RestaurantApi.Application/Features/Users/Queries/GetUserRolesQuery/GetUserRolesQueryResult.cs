namespace RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;

// Rol durumu listesinin her bir satırı: tüm roller döner, HasRole kullanıcının o role sahip olup olmadığını bildirir.
public record GetUserRolesQueryResult()
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool HasRole { get; set; }
}
