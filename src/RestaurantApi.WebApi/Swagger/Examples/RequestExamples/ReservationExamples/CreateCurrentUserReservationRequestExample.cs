using RestaurantApi.Application.Features.Reservation.Commands.CreateCurrentUserReservationCommand;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.ReservationExamples;

public class CreateCurrentUserReservationRequestExample: IExamplesProvider<CreateCurrentUserReservationCommand>
{
    public CreateCurrentUserReservationCommand GetExamples()
    {
        // start-time ve end-time YEREL saat (Avrupa/İstanbul, 24 saatlik) olarak gönderilir
        var localToday = DateTime.UtcNow.Add(ReservationSchedule.LocalOffset).Date;

        return new CreateCurrentUserReservationCommand(
            GuestCount: 4,
            StartTime: localToday.AddHours(19),
            EndTime: localToday.AddHours(20),
            TableId: Guid.NewGuid(),
            Note: "Pencere kenarında bir masa tercih ediyoruz."
        );
    }
}
