using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class CheckoutSessionCreatedResponseExample: IExamplesProvider<CheckoutResponse>
{
    public CheckoutResponse GetExamples()
    {
        return new CheckoutResponse
        {
            Message = "Sipariş oluşturuldu, ödeme oturumu açıldı.",
            Code = Codes.CHECKOUT_SESSION_CREATED,
            SessionUrl = "https://checkout.stripe.com/c/pay/cs_test_a1B2c3D4e5F6g7H8i9J0"
        };
    }
}
