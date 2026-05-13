namespace RestaurantApi.Application.Features.Files.Dtos;

public class TokenUserModel
{
    public Guid Id { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}