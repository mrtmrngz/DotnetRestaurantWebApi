using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;

public record GetDiscountDetailQuery(Guid Id)
    : IRequest<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>;
