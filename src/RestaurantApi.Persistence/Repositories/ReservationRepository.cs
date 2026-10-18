using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Repositories;

public class ReservationRepository: IReservationRepository
{
    private readonly ApiContext _context;

    public ReservationRepository(ApiContext context)
    {
        _context = context;
    }

    public IQueryable<Reservation> GetAllAsQueryable()
    {
        return _context.Reservations.AsQueryable();
    }

    // Bitiş zamanı geçmiş rezervasyonlar (kapatılmayı unutulmuş / teknik hata) aktif sayılmaz.

    public Task<bool> HasActiveReservationAsync(Guid restaurantTableId, DateTime now, CancellationToken ctx)
    {
        return _context.Reservations
            .AnyAsync(r => r.RestaurantTableId == restaurantTableId &&
                           (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                           r.EndTime >= now, ctx);
    }

    public async Task<int> GetMaxActiveGuestCountAsync(Guid restaurantTableId, DateTime now, CancellationToken ctx)
    {
        var maxGuestCount = await _context.Reservations
            .Where(r => r.RestaurantTableId == restaurantTableId &&
                        (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                        r.EndTime >= now)
            .MaxAsync(r => (int?)r.GuestCount, ctx);

        return maxGuestCount ?? 0;
    }

    // Bitiş zamanı geçmiş rezervasyonlar meşgul sayılmaz (masa güncelleme/silme kontrolüyle aynı kural).

    public async Task<List<Guid>> GetBusyTableIdsAsync(DateTime now, CancellationToken ctx)
    {
        return await _context.Reservations
            .Where(r => (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                        r.EndTime >= now)
            .Select(r => r.RestaurantTableId)
            .Distinct()
            .ToListAsync(ctx);
    }
}
