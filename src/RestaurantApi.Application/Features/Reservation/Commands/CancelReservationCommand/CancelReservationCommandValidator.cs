using FluentValidation;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Features.Reservation.Commands.CancelReservationCommand;

public class CancelReservationCommandValidator: AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Rezervasyon id boş olamaz.");
        
        When(x => x.RequestType == CancelReservationTypes.Admin, () =>
        {
            RuleFor(x => x.UserId)
                .Null()
                .WithMessage("Admin tarafından yapılan iptal işlemlerinde kullanıcı id gönderilemez.");
        });

        When(x => x.RequestType == CancelReservationTypes.CurrentUser, () =>
        {
            RuleFor(x => x.UserId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("Kullanıcı tarafından yapılan iptal işlemlerinde kullanıcı id boş olamaz.");
        });
    }
}