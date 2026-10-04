using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

public record PublicProductsListQuery(): IRequest<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>;