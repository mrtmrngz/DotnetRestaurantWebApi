using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;

public record CreateOnlineCheckoutCommand() : IRequest<CheckoutResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid? UserId { get; init; }
    
    [BindNever]
    [JsonIgnore]
    public Guid? RestaurantTableId { get; init; }
    
    [BindNever]
    [JsonIgnore]
    public string IdempotencyKey { get; init; }

    [BindNever]
    [JsonIgnore]
    public CheckoutRequestTypes CheckoutRequestTypes { get; init; } = CheckoutRequestTypes.CurrentUser;
};