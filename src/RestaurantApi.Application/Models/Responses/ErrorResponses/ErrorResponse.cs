using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Models.Responses.ErrorResponses;

public class ErrorResponse: BaseResponse
{
    public ErrorResponse(string message, Codes code)
    {
        Message = message;
        Code = code;
    }     
}