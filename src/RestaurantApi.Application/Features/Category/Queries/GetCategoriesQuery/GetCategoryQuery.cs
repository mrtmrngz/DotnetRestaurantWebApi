using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;

public record GetCategoryQuery(): IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>>;