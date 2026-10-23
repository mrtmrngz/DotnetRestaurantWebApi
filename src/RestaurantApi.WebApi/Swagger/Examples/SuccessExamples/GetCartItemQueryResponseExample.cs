using RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetCartItemQueryResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>> GetExamples()
    {
        List<GetCartQueryResult> items = new List<GetCartQueryResult>
        {
            new GetCartQueryResult(Title: "Adana Kebap", Quantity:2, UnitPrice:450m, SubTotal:900m, DiscountRate:20, TotalPrice:720m),
            new GetCartQueryResult(Title: "Mercimek Çorbası", Quantity:1, UnitPrice:150m, SubTotal:150m, DiscountRate:0, TotalPrice:150m),
            new GetCartQueryResult(Title: "Baklava", Quantity:2, UnitPrice:450m, SubTotal:900m, DiscountRate:20, TotalPrice:720m)
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>(data: items);
    }
}