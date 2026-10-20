using RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;
using RestaurantApi.Domain.Entities;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.ReservationExamples;

public class ReservationStatusUpdateRequestExample: IExamplesProvider<ReservationStatusUpdateCommand>
{
    public ReservationStatusUpdateCommand GetExamples()
    {
        return new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);
    }
}