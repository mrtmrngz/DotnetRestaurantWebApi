using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class TableCashCheckoutResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
        {
            Message = "Masa siparişi oluşturuldu, ödeme nakit olarak alınacaktır.",
            Code = Codes.CHECKOUT_SUCCESS
        };
    }
}
