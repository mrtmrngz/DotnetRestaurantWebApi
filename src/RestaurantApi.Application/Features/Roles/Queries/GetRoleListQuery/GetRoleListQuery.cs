using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;

// Rol listesi sorgusu: parametre almaz, sistemdeki tüm rolleri listeler.
public record GetRoleListQuery: IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>>;
