using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.DeleteRestaurantTableCommand;

public record DeleteRestaurantTableCommand: IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid TableId { get; init; }
}
