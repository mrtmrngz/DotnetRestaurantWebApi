using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;

public record GetDiscountListQuery(DiscountStatus? Status = null)
    : IRequest<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>;
