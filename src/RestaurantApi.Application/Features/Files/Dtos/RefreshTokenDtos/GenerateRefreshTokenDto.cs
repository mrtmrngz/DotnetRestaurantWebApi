namespace RestaurantApi.Application.Features.Files.Dtos.RefreshTokenDtos;

public class GenerateRefreshTokenDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}