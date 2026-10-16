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
}
