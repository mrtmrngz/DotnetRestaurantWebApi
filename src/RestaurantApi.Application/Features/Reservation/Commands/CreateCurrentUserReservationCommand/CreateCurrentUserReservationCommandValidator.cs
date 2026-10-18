using FluentValidation;
using RestaurantApi.Application.Features.Rules.ReservationRules;

namespace RestaurantApi.Application.Features.Reservation.Commands.CreateCurrentUserReservationCommand;

public class CreateCurrentUserReservationCommandValidator: AbstractValidator<CreateCurrentUserReservationCommand>
{
    private const int MaxNoteLength = 350;

    private readonly TimeProvider _timeProvider;

    public CreateCurrentUserReservationCommandValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        RuleFor(x => x.TableId)
            .NotEmpty()
            .WithMessage("Masa seçilmelidir.");

        RuleFor(x => x.GuestCount)
            .NotEmpty()
            .WithMessage("Misafir sayısı zorunludur.")
            .GreaterThan(0)
            .WithMessage("Misafir sayısı 0'dan büyük olmalıdır.");

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength)
            .WithMessage($"Not en fazla {MaxNoteLength} karakter olabilir.");

        // Zaman kuralları: gelen start-time/end-time YEREL saat (Avrupa/İstanbul, 24 saatlik) kabul edilir.
        // açılıştan en az 1 saat sonrası (11:00) -> şuandan en az 1 saat sonrası -> kapanıştan en fazla 1.30 saat öncesi (22:30)
        RuleFor(x => x.StartTime)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Başlangıç zamanı zorunludur.")
            .Must(startTime => startTime >= ReservationSchedule.GetEarliestStartTime(startTime))
            .WithMessage(
                "Rezervasyon başlangıç zamanı restoran açılışından en az 1 saat sonrası olmalıdır (en erken 11:00).")
            .Must(startTime => startTime >= GetLocalNow() + ReservationSchedule.MinStartTimeOffsetFromNow)
            .WithMessage("Rezervasyon başlangıç zamanı şuandan en az 1 saat sonrası olmalıdır.")
            .Must(startTime => startTime <= ReservationSchedule.GetLatestStartTime(startTime))
            .WithMessage(
                "Rezervasyon başlangıç zamanı restoran kapanışından en fazla 1.30 saat önce olabilir (en geç 22:30).");

        // Bitiş zamanı: başlangıçtan sonra, kapanıştan en fazla 30 dakika önce (en geç 23:30) ve süre en fazla 3 saat
        RuleFor(x => x.EndTime)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Bitiş zamanı zorunludur.")
            .Must((command, endTime) => endTime > command.StartTime)
            .WithMessage("Bitiş zamanı başlangıç zamanından sonra olmalıdır.")
            .Must((command, endTime) => endTime <= GetLatestEndTimeOfStartDay(command.StartTime))
            .WithMessage(
                "Rezervasyon bitiş zamanı, başlangıç günü için en geç 23:30 olabilir (kapanıştan en fazla 30 dakika önce).")
            .Must((command, endTime) => command.StartTime == default ||
                                        endTime - command.StartTime <= ReservationSchedule.MaxReservationDuration)
            .WithMessage("Rezervasyon süresi en fazla 3 saat olabilir.");
    }

    #region Helper Methods

    private DateTime GetLocalNow()
    {
        return ReservationSchedule.ToLocalTime(_timeProvider.GetUtcNow().UtcDateTime);
    }

    // Başlangıç zamanı gönderilmediyse (default) kural atlanır; aksi halde başlangıç gününün en geç bitiş anı (23:30) döner.
    // Böylece gece yarısını geçen (örn. 22:00 -> ertesi gün 01:00) rezervasyonlar kabul edilmez.
    private static DateTime GetLatestEndTimeOfStartDay(DateTime startTime)
    {
        return startTime == default ? DateTime.MaxValue : ReservationSchedule.GetLatestEndTime(startTime);
    }

    #endregion
}
