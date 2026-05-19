using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class ContentCreatedExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse{ Message = "Kayıt başarılı.", Code = Codes.CONTENT_CREATED_SUCCESS};
    }
}