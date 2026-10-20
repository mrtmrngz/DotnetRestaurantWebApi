using MediatR;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Commands.CancelReservationCommand;

public record CancelReservationCommand(
    Guid ReservationId,
    Guid? UserId,
    CancelReservationTypes RequestType = CancelReservationTypes.CurrentUser
    ): IRequest<BaseResponse>;