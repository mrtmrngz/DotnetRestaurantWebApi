using RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetPanelOrderDetailResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult> GetExamples()
    {
        // Örnek: kullanıcı siparişi (müşteri adı ve adres anlık görüntüsü dolu, masa numarası boş).
        // Masa siparişlerinde ise masa numarası dolu, müşteri adı ve adres boş olur.
        var mockData = new GetPanelOrderDetailQueryResult
        {
            OrderId = Guid.NewGuid(),
            SubTotal = 450m,
            ShippingCost = 0m,
            Total = 450m,
            OrderDate = DateTime.UtcNow.AddHours(-3),
            OrderStatus = OrderStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid,
            AddressSnapshot = "{\"UserId\":\"7b0e6a1c-5d2f-4b8a-9e3c-1a2b3c4d5e6f\"," +
                              "\"Id\":\"1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f\"," +
                              "\"RecipientName\":\"Ayşe Yılmaz\",\"BuildingInfo\":\"A Blok\"," +
                              "\"BuildingNumber\":\"12\",\"City\":\"İstanbul\",\"Neighborhood\":\"Caferağa\"," +
                              "\"PhoneNumber\":\"05321112233\",\"Street\":\"Moda Caddesi\",\"Town\":\"Kadıköy\"," +
                              "\"ZipCode\":\"34710\"," +
                              "\"FullAddress\":\"Caferağa Mah. Moda Cad. No:12 A Blok 34710 Kadıköy/İstanbul\"}",
            TableNumber = null,
            CustomerFullName = "Ayşe Yılmaz",
            OrderItems = new List<PanelOrderItemDetail>
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

        return new GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>(data: mockData);
    }
}
