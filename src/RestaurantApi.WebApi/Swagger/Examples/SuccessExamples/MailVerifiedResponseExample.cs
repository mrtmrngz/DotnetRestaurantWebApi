using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class MailVerifiedResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse()
        {
            Code = Codes.MAIL_VERIFIED_SUCCESS,
            Message = "Mail adresiniz başarıyla doğrulandı, giriş yapabilirsiniz."
        };
    }
}