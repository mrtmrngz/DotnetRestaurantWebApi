using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;

public record ProcessStripeWebhookCommand : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public string Signature { get; init; } = null!;

    [BindNever]
    [JsonIgnore]
    public string Payload { get; init; } = null!;
}
