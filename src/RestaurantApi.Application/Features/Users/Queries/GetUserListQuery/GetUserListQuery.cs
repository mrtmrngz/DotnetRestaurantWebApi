using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;

// Panel kullanıcı listesi sorgusu: parametre almaz, tüm kullanıcıları rolleriniyle listeler.
public record GetUserListQuery
    : IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>>;
