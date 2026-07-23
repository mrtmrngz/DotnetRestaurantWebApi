using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Address.Queries.GetUserAddressByIdQuery;

public record GetUserAddressByIdQuery
    (Guid UserId, Guid AddressId): IRequest<GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>>;