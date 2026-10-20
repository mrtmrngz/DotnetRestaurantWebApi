using RestaurantApi.Application.Features.Reservation.Commands.CreatePanelReservationCommand;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.ReservationExamples;

public class CreatePanelReservationRequestExample:IExamplesProvider<CreatePanelReservationCommand>
{
    public CreatePanelReservationCommand GetExamples()
    {
        var localToday = DateTime.UtcNow.Add(ReservationSchedule.LocalOffset).Date;

        return new CreatePanelReservationCommand(
            GuestCount: 4,
            FullName: "John Doe",
            PhoneNumber: "+905441234567",
            Email: "john@gmail.com",
            StartTime: localToday.AddHours(19),
            EndTime: localToday.AddHours(20),
            TableId: Guid.NewGuid(),
            Note: "Pencere kenarında bir masa tercih ediyoruz."
        );
    }
}