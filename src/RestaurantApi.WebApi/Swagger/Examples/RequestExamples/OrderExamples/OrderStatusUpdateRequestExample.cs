using RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;
using RestaurantApi.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.OrderExamples;

// Sipariş durum güncelleme isteğinin örnek gövdesi.
// OrderId route'tan geldiği için (JsonIgnore/BindNever) örnekte yalnızca gövde alanları bulunur.
public class OrderStatusUpdateRequestExample: IExamplesProvider<OrderStatusUpdateCommand>
{
    public OrderStatusUpdateCommand GetExamples()
    {
        return new OrderStatusUpdateCommand(
            UserId: Guid.NewGuid(),
            NewStatus: OrderStatus.Confirmed
        );
    }
}
