using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;

public record AdminCategoryListQuery(): IRequest<GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>>;