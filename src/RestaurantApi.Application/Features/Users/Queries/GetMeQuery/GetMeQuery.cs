using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetMeQuery;

public record GetMeQuery(string UserId): IRequest<GeneralSuccessResponseWithData<GetMeQueryResult>>;