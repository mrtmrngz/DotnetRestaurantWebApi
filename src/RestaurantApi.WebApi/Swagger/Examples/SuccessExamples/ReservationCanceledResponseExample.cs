using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class ReservationCanceledResponseExample : IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
            { Message = "Rezervasyon başarılı bir şekilde iptal edildi.", Code = Codes.RESERVATION_CANCELLED_SUCCESS };
    }
}