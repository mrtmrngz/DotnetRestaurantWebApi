using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class MailSentResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
        {
            Message = "Yanıt mesajı",
            Code = Codes.MAIL_SENT_SUCCESS
        };
    }
}