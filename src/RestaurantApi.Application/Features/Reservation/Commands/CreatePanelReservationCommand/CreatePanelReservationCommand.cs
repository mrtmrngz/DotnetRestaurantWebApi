using MediatR;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Commands.CreatePanelReservationCommand;

public record CreatePanelReservationCommand(
    int GuestCount,
    string FullName,
    string PhoneNumber,
    string Email,
    DateTime StartTime,
    DateTime EndTime,
    Guid TableId,
    string? Note = null
) : IRequest<BaseResponse>;