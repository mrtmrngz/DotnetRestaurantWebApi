using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IReservationRepository
{
    IQueryable<Reservation> GetAllAsQueryable();
    Task<bool> HasActiveReservationAsync(Guid restaurantTableId, DateTime now, CancellationToken ctx);
    Task<int> GetMaxActiveGuestCountAsync(Guid restaurantTableId, DateTime now, CancellationToken ctx);
}
