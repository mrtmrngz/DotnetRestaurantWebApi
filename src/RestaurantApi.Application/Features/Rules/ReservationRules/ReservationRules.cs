using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.ReservationRules;

public class ReservationRules
{
    private readonly ILogger<ReservationRules> _logger;

    public ReservationRules(ILogger<ReservationRules> logger)
    {
        _logger = logger;
    }

    public void ShouldTableBeActiveForReservation(bool isActive, Guid tableId, bool isPanelRequest)
    {
        if (isActive)
            return;

        _logger.LogWarning(
            "Pasif masa için rezervasyon oluşturulamaz. TableId:{TableId}, IsPanelRequest:{IsPanelRequest}",
            tableId, isPanelRequest);

        if (isPanelRequest)
            throw new ConflictException("Pasif masa için rezervasyon oluşturulamaz.");

        throw new NotFoundException("Masa bulunamadı.");
    }

    public void ShouldTableCapacityBeEnough(int capacity, int guestCount, Guid tableId)
    {
        if (guestCount <= capacity)
            return;

        _logger.LogWarning(
            "Masa kapasitesi misafir sayısı için yetersiz. TableId:{TableId}, Capacity:{Capacity}, GuestCount:{GuestCount}",
            tableId, capacity, guestCount);
        throw new UnprocessableEntityError(
            $"Seçilen masanın kapasitesi yetersiz. Masa kapasitesi: {capacity}, misafir sayısı: {guestCount}.");
    }

    // Masa yalnızca istenen zaman aralığıyla çakışan bir aktif rezervasyonu varsa kilitlidir:
    // örn. masa 12:00-13:00 arası doluysa 13:00'te başlayan yeni rezervasyon çakışmaz;
    // farklı güne ait rezervasyonlar (10 Mayıs vs 11 Mayıs) hiçbir zaman çakışmaz.
    public void ShouldTableNotHaveConflictingReservation(bool hasConflictingReservation, Guid tableId,
        DateTime startTime, DateTime endTime)
    {
        if (!hasConflictingReservation)
            return;

        _logger.LogWarning(
            "Seçilen masa istenen zaman aralığında dolu. TableId:{TableId}, StartTime:{StartTime}, EndTime:{EndTime}",
            tableId, startTime, endTime);
        throw new ConflictException("Seçilen masa, istenen zaman aralığında dolu.");
    }
}
