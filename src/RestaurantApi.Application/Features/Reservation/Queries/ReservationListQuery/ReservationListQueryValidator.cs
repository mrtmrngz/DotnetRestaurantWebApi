using FluentValidation;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

public sealed class ReservationListQueryValidator
    : AbstractValidator<ReservationListQuery>
{
    public ReservationListQueryValidator()
    {
        RuleFor(x => x.RequestType)
            .IsInEnum()
            .WithMessage("Geçersiz rezervasyon listeleme türü.");

        When(x => x.RequestType == ReservationListRequestEnums.PanelUser, () =>
        {
            RuleFor(x => x.UserId)
                .Null()
                .WithMessage(
                    "Panel kullanıcısı tarafından yapılan listeleme işlemlerinde kullanıcı ID gönderilemez.");
        });

        When(x => x.RequestType == ReservationListRequestEnums.CurrentUser, () =>
        {
            RuleFor(x => x.UserId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage(
                    "Kullanıcı tarafından yapılan listeleme işlemlerinde kullanıcı ID boş olamaz.");
        });
    }
}