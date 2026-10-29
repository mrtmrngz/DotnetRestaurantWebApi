using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class StripeWebhookProcessedResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
        {
            Message = "Ödeme onaylandı, sipariş ödendi olarak işaretlendi.",
            Code = Codes.STRIPE_WEBHOOK_PROCESSED
        };
    }
}
