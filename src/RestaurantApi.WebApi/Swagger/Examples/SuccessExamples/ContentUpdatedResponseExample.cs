using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class ContentUpdatedResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse { Message = "Durum mesajı", Code = Codes.CONTENT_UPDATED_SUCCESS };
    }
}