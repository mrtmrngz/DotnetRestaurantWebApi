using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;

public record CategoryDetailQuery(Guid CategoryId): IRequest<GeneralSuccessResponseWithData<CategoryDetailQueryResult>>;