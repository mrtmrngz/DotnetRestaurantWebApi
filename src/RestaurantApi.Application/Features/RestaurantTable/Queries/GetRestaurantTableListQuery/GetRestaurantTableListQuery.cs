using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;

public record GetRestaurantTableListQuery
    : IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>
{
    [FromQuery(Name = "is-active")]
    public bool? IsActive { get; init; }

    [FromQuery(Name = "min-guest")]
    public int? MinGuest { get; init; }

    [FromQuery(Name = "max-guest")]
    public int? MaxGuest { get; init; }

    [FromQuery(Name = "number")]
    public string? Number { get; init; }
}
