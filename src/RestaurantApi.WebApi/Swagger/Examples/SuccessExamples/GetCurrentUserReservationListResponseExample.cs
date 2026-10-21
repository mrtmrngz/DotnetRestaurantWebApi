using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetCurrentUserReservationListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>> GetExamples()
    {
        var mockData = new List<ReservationListQueryResult>
        {
            new ReservationListQueryResult
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Email = "john@gmail.com",
                EndTime = DateTime.UtcNow.AddDays(1),
                FullName = "John Doe",
                GuestCount = 5,
                Note = null,
                PhoneNumber = "+901234567890",
                RestaurantTableNumber = 1,
                StartTime = DateTime.UtcNow.AddDays(-2),
                Status = ReservationStatus.Confirmed
            },
            new ReservationListQueryResult
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Email = "jane@gmail.com",
                EndTime = DateTime.UtcNow.AddDays(-1),
                FullName = "Jane Doe",
                GuestCount = 2,
                Note = null,
                PhoneNumber = "+901234567890",
                RestaurantTableNumber = 3,
                StartTime = DateTime.UtcNow.AddDays(-2),
                Status = ReservationStatus.Completed
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>(data: mockData);
    }
}