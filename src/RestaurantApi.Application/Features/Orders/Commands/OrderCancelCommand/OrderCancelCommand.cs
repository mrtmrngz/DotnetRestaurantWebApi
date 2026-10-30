using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;

public record OrderCancelCommand : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid OrderId { get; init; }

    [JsonIgnore]
    [BindNever]
    public Guid UserId { get; init; }
}
