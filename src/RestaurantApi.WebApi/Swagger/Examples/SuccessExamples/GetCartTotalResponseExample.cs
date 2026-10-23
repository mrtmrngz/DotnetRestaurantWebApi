using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetCartTotalResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetCartTotalQueryResult>>
{
    public GeneralSuccessResponseWithData<GetCartTotalQueryResult> GetExamples()
    {
        GetCartTotalQueryResult total = new GetCartTotalQueryResult(Subtotal: 150m, AfterDiscountTotal: 120m,
            SavingTotal: 30m, ShippingCost: 120m, Total: 240m);

        return new GeneralSuccessResponseWithData<GetCartTotalQueryResult>(data: total);
    }
}