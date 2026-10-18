using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Commands.CreateCurrentUserReservationCommand;

public record CreateCurrentUserReservationCommand(
    int GuestCount,
    DateTime StartTime,
    DateTime EndTime,
    Guid TableId,
    string? Note = null
) : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public Guid UserId { get; init; }
}
