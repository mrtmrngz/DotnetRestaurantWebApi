using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class GeneralSuccessResponseWithData<T>: BaseResponse
{
    public T Data { get; set; }

    public GeneralSuccessResponseWithData(T data, string message = "Success", Codes code = Codes.FETH_DATA_SUCCESS)
    {
        Message = message;
        Code = code;
        Data = data;
    }
}