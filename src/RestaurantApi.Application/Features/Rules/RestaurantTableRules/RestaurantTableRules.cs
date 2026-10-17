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
