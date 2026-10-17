using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.UpdateRestaurantTableCommand;

public record UpdateRestaurantTableCommand(
    int? Number = null,
    int? Capacity = null,
    bool? IsActive = null
) : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid TableId { get; init; }
}
