using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class ForgotPasswordApplyResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
        {
            Message = "Şifreniz başarılı bir şekilde sıfırlandı, lütfen giriş yapınız.",
            Code = Codes.PASSWORD_RESET_SUCCESS
        };
    }
}