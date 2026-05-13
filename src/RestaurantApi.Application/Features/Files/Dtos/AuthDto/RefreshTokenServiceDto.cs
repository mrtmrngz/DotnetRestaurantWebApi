namespace RestaurantApi.Application.Features.Files.Dtos.AuthDto;

public class RefreshTokenServiceDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}