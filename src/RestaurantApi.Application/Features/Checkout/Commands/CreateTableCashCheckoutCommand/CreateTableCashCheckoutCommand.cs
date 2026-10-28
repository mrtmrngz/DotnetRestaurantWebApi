using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateTableCashCheckoutCommand;

public record CreateTableCashCheckoutCommand : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid RestaurantTableId { get; init; }

    [BindNever]
    [JsonIgnore]
    public string IdempotencyKey { get; init; } = null!;
}
