namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class LoginControllerResponse: BaseResponse
{
    public string? AccessToken { get; set; }
}