using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

public record GetUserOrdersQuery: IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>>
{
    [JsonIgnore]
    [BindNever]
    public Guid UserId { get; init; }
}
