namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class JwtAndRefreshTokenResult
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}