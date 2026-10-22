using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Cart.Commands.RemoveCartCommand;

public record RemoveCartCommand() : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid? UserId { get; init; }
    
    [BindNever]
    [JsonIgnore]
    public Guid? RestaurantTableId { get; init; }

    [BindNever] 
    [JsonIgnore] 
    public CartRequestTypes RequestType { get; init; } = CartRequestTypes.CurrentUser;
};