using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.RestaurantTableRules;

public class RestaurantTableRules
{
    private readonly ILogger<RestaurantTableRules> _logger;

    public RestaurantTableRules(ILogger<RestaurantTableRules> logger)
    {
        _logger = logger;
    }

    public void ShouldTableNumberBeUnique(bool isNumberTaken, int tableNumber)
    {
        if (isNumberTaken)
        {
            _logger.LogWarning("Masa numarası zaten kullanılıyor. TableNumber:{TableNumber}", tableNumber);
            throw new ConflictException("Bu masa numarası zaten kullanılmaktadır.");
        }
    }

    public void ShouldTableExist(Domain.Entities.RestaurantTable? table, Guid tableId)
    {
        if (table is null)
        {
            _logger.LogWarning("Aranılan masa bulunamadı. TableId:{TableId}", tableId);
            throw new NotFoundException("Masa bulunamadı.");
        }
    }

    public void ShouldTableBeUpdatableWithReservations(bool hasActiveReservation, int maxActiveGuestCount,
        int? requestedCapacity, bool? requestedIsActive)
    {
        if (!hasActiveReservation)
            return;

        if (requestedIsActive is false)
        {
            _logger.LogWarning("Aktif rezervasyonu bulunan masa pasife alınamaz. MaxGuestCount:{MaxGuestCount}",
                maxActiveGuestCount);
            throw new ConflictException("Aktif rezervasyonu bulunan masa pasife alınamaz.");
        }

        if (requestedCapacity.HasValue && requestedCapacity.Value < maxActiveGuestCount)
        {
            _logger.LogWarning(
                "Masa kapasitesi aktif rezervasyonlardaki en yüksek misafir sayısının altına düşürülemez. Capacity:{Capacity}, MaxGuestCount:{MaxGuestCount}",
                requestedCapacity, maxActiveGuestCount);
            throw new ConflictException(
                $"Masa kapasitesi, aktif rezervasyonlardaki en yüksek misafir sayısından ({maxActiveGuestCount}) küçük olamaz.");
        }
    }

    public void ShouldTableNotHaveActiveReservationForDeletion(bool hasActiveReservation, Guid tableId)
    {
        if (!hasActiveReservation)
            return;

        _logger.LogWarning("Aktif rezervasyonu bulunan masa silinemez. TableId:{TableId}", tableId);
        throw new ConflictException("Aktif rezervasyonu bulunan masa silinemez.");
    }

    public void ShouldTableBeValidForDeletion(Domain.Entities.RestaurantTable? table)
    {
        if (table is null || !table.IsActive)
        {
            _logger.LogWarning("Silinmek istenen masa bulunamadı veya zaten pasif durumda. TableId:{TableId}",
                table?.Id);
            throw new NotFoundException("Masa bulunamadı.");
        }
    }
}
