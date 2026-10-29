using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;

// Kullanıcı siparişinin durumunu güncelleme komutu.
// UserId ve NewStatus istek gövdesinden gelir; OrderId ise route'tan controller tarafından set edilir,
// bu yüzden gövdeden okunmasını engellemek için [JsonIgnore] + [BindNever] ile korunur.
public record OrderStatusUpdateCommand(
    Guid UserId,
    OrderStatus NewStatus
) : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid OrderId { get; init; }
}
