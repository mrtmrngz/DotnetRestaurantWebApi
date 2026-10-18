using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Repositories;

public interface IReservationRepository
{
    IQueryable<Reservation> GetAllAsQueryable();
    void CreateReservation(Reservation reservation, CancellationToken ctx);
    Task<int> GetMaxActiveGuestCountAsync(Guid restaurantTableId, DateTime now, CancellationToken ctx);
    Task<List<Guid>> GetBusyTableIdsAsync(DateTime now, CancellationToken ctx);
}
