using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetTableOrdersResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>> GetExamples()
    {
        var mockData = new List<GetTableOrdersQueryResult>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 600m,
                ShippingCost = 0m,
                Total = 600m,
                OrderStatus = OrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 250m,
                ShippingCost = 0m,
                Total = 250m,
                OrderStatus = OrderStatus.Delivered,
                OrderDate = DateTime.UtcNow.AddDays(-1)
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>(data: mockData);
    }
}
