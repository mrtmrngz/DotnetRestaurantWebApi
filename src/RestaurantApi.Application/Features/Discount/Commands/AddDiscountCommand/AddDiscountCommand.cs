using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;

public record AddDiscountCommand(
    Guid ProductId,
    double DiscountRate,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<BaseResponse>;
