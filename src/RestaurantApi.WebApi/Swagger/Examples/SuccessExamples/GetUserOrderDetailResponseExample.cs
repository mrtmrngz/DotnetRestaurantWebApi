using RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserOrderDetailResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult> GetExamples()
    {
        var mockData = new GetUserOrderDetailQueryResult
        {
            OrderId = Guid.NewGuid(),
            OrderStatus = OrderStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid,
            SubTotal = 450m,
            ShippingCost = 0m,
            Total = 450m,
            AddressSnapshot = "{\"UserId\":\"7b0e6a1c-5d2f-4b8a-9e3c-1a2b3c4d5e6f\"," +
                              "\"Id\":\"1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f\"," +
                              "\"RecipientName\":\"Ayşe Yılmaz\",\"BuildingInfo\":\"A Blok\"," +
                              "\"BuildingNumber\":\"12\",\"City\":\"İstanbul\",\"Neighborhood\":\"Caferağa\"," +
                              "\"PhoneNumber\":\"05321112233\",\"Street\":\"Moda Caddesi\",\"Town\":\"Kadıköy\"," +
                              "\"ZipCode\":\"34710\"," +
                              "\"FullAddress\":\"Caferağa Mah. Moda Cad. No:12 A Blok 34710 Kadıköy/İstanbul\"}",
            OrderItems = new List<OrderItemDetail>
            {
                new()
                {
                    ProductName = "Izgara Somon",
                    UnitPrice = 320m,
                    Quantity = 1
                },
                new()
                {
                    ProductName = "Limonata",
                    UnitPrice = 65m,
                    Quantity = 2
                }
            }
        };

        return new GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>(data: mockData);
    }
}
