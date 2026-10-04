using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;

public record AdminProductListQuery(): IRequest<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>;