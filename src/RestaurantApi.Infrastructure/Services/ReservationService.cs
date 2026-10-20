using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Infrastructure.Services;

public class ReservationService: IReservationService
{
    private readonly ILogger<ReservationService> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly ReservationRules _reservationRules;

    public ReservationService(ILogger<ReservationService> logger,
        IRestaurantTableRepository restaurantTableRepository, IReservationRepository reservationRepository,
        RestaurantTableRules restaurantTableRules, ReservationRules reservationRules)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _reservationRepository = reservationRepository;
        _restaurantTableRules = restaurantTableRules;
        _reservationRules = reservationRules;
    }

    public async Task ShouldTableBeReservableAsync(Guid tableId, int guestCount, DateTime startTimeUtc,
        DateTime endTimeUtc, DateTime nowUtc, bool isPanelRequest, CancellationToken ctx)
    {
        _logger.LogInformation(
            "Masa rezervasyona uygunluk kontrolü yapılıyor. TableId:{TableId}, GuestCount:{GuestCount}, StartTime:{StartTime}, EndTime:{EndTime}, IsPanelRequest:{IsPanelRequest}",
            tableId, guestCount, startTimeUtc, endTimeUtc, isPanelRequest);

        // masa satırı transaction bitene kadar kilitli kalır (SELECT ... FOR UPDATE)

        var table = await _restaurantTableRepository.GetByIdForUpdateAsync(tableId, ctx);

        _restaurantTableRules.ShouldTableExist(table, tableId);

        _reservationRules.ShouldTableBeActiveForReservation(table!.IsActive, tableId, isPanelRequest);

        _reservationRules.ShouldTableCapacityBeEnough(table.Capacity, guestCount, tableId);

        // aktif rezervasyon: Pending/Confirmed ve bitiş zamanı geçmemiş.
        // çakışma: mevcut rezervasyon [StartTime, EndTime) aralığıyla kesişiyorsa masa yalnızca o saatler için kilitlidir.
        // örn. 12:00-13:00 dolu ise 13:00'te başlayan rezervasyon çakışmaz; farklı günler hiçbir zaman çakışmaz.

        var hasConflictingReservation = await _reservationRepository.GetAllAsQueryable()
            .AnyAsync(r => r.RestaurantTableId == tableId &&
                           (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed) &&
                           r.EndTime >= nowUtc &&
                           r.StartTime < endTimeUtc &&
                           r.EndTime > startTimeUtc, ctx);

        _reservationRules.ShouldTableNotHaveConflictingReservation(hasConflictingReservation, tableId, startTimeUtc,
            endTimeUtc);

        _logger.LogInformation("Masa rezervasyona uygun. TableId:{TableId}, TableNumber:{TableNumber}",
            table.Id, table.Number);
    }
}
