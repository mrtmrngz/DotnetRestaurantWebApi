using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;

public record GetRestaurantTableDetailQuery(Guid Id)
    : IRequest<GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>>;
