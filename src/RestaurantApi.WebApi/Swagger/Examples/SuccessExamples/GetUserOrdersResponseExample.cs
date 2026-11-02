using RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserOrdersResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>> GetExamples()
    {
        var mockData = new List<GetUserOrdersQueryResult>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 450m,
                ShippingCost = 0m,
                Total = 450m,
                OrderStatus = OrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow.AddHours(-3)
            },
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 320m,
                ShippingCost = 120m,
                Total = 440m,
                OrderStatus = OrderStatus.Delivered,
                OrderDate = DateTime.UtcNow.AddDays(-2)
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>(data: mockData);
    }
}
