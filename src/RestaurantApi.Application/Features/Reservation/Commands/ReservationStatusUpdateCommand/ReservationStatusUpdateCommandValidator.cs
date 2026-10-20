using FluentValidation;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;

public class ReservationStatusUpdateCommandValidator: AbstractValidator<ReservationStatusUpdateCommand>
{
    public ReservationStatusUpdateCommandValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Rezervasyon ID bilgisi zorunludur.");

        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Rezervasyon durumu zorunludur.")
            .Must(ReservationStatus.IsValid)
            .WithMessage("Geçersiz rezervasyon durumu.");
    }
}