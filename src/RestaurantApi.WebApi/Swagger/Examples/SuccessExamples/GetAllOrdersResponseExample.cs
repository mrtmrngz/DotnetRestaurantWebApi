using RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetAllOrdersResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>> GetExamples()
    {
        var mockData = new List<GetAllOrdersQueryResult>
        {
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 450m,
                ShippingCost = 120m,
                Total = 570m,
                OrderStatus = OrderStatus.Confirmed,
                OrderDate = DateTime.UtcNow.AddHours(-1),
                UserId = Guid.NewGuid(),
                TableId = null,
                Type = GetAllOrdersQueryResult.OwnerUser
            },
            new()
            {
                OrderId = Guid.NewGuid(),
                SubTotal = 600m,
                ShippingCost = 0m,
                Total = 600m,
                OrderStatus = OrderStatus.Preparing,
                OrderDate = DateTime.UtcNow.AddHours(-2),
                UserId = null,
                TableId = Guid.NewGuid(),
                Type = GetAllOrdersQueryResult.OwnerTable
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>(data: mockData);
    }
}
