namespace RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;

// Panel kullanıcı listesinin her bir satırı.
// TwoFactorStatus, AppUser.TwoFactorEnabled alanından gelir; Roles kullanıcının rol ADLARIDIR.
public record GetUserListQueryResult()
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorStatus { get; set; }
    public List<string> Roles { get; set; } = new();
}
