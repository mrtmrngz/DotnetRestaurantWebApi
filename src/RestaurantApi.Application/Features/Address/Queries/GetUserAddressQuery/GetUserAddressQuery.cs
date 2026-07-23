using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;

public record GetUserAddressQuery(Guid UserId): IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>>;