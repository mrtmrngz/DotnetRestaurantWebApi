using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;

public record ProfileInfoQuery(Guid UserId): IRequest<GeneralSuccessResponseWithData<ProfileInfoQueryResult>>;