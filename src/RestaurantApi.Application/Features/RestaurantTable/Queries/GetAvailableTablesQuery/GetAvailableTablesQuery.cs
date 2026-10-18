using MediatR;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;

public record GetAvailableTablesQuery
    : IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>
{
    [FromQuery(Name = "guest-count")]
    public int GuestCount { get; init; }

    [FromQuery(Name = "start-time")]
    public DateTime StartTime { get; init; }

    [FromQuery(Name = "end-time")]
    public DateTime EndTime { get; init; }
}
