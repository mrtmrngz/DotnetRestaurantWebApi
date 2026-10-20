using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;

public record ReservationStatusUpdateCommand(
    string Status
) : IRequest<BaseResponse>
{
    [JsonIgnore]
    [BindNever]
    public Guid ReservationId { get; init; }
};