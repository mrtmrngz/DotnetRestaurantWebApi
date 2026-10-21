using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;

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

    public void ShouldUserIdExistOnCurrentUserReservationCancel(Guid? userId, Guid reservationId)
    {
        if (userId != null)
            return;

        _logger.LogWarning(
            "Bu işlemi gerçekleştirmek için kullanıcı id gereklidir. UserId:{usId} ReservationId:{resId}", userId,
            reservationId);

        throw new BadRequestException("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }

    public void ShouldReservationExist(Domain.Entities.Reservation? reservation, Guid reservationId)
    {
        if (reservation is not null)
            return;

        _logger.LogWarning("İlgili rezervasyon sistemde bulunamadı. ReservationId: {resId}", reservationId);
        throw new NotFoundException("Rezervasyon bulunamadı.");
    }

    public void ShouldReservationNotCanceledOrCompleted(string status, Guid reservationId, CancelReservationTypes types)
    {
        if(types == CancelReservationTypes.Admin)
            return;
        
        if (string.Equals(status, nameof(ReservationStatus.Canceled), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rezervasyon zaten iptal edilmiş. ReservationId:{resId}", reservationId);
            throw new ConflictException("Rezervasyon zaten iptal edilmiş.");
        }
        
        if (string.Equals(status, nameof(ReservationStatus.Completed), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rezervasyon tamamlanmış durumda. ReservationId:{resId}", reservationId);
            throw new UnprocessableEntityError("Rezervasyon tamamlandığı için iptal edilemez.");
        }
    }

    public void ShouldReservationEndTimeNotPassing(DateTime endTime, DateTimeOffset nowUtc, Guid reservationId)
    {
        if (endTime <= nowUtc)
        {
            _logger.LogInformation("Rezervasyon bitiş zamanı zaten geçmiş. ReservationId:{resId}", reservationId);
            throw new UnprocessableEntityError("Rezervasyon bitiş tarihi zaten geçmiş.");
        }
    }

    public void UserIdShouldInListRequest(Guid? userId)
    {
        if(userId != null)
            return;
        
        _logger.LogInformation("Rezervasyon listeleme sırasında user id null geliyor.");
        throw new BadRequestException("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }
}