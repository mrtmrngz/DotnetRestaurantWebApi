namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class LoginResponse: BaseResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}