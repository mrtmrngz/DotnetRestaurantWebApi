using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.CheckoutRules;

public class CheckoutRules
{
    private readonly ILogger<CheckoutRules> _logger;

    public CheckoutRules(ILogger<CheckoutRules> logger)
    {
        _logger = logger;
    }

    public void ShouldIdempotencyKeyNotExist(bool exist)
    {
        if(!exist)
            return;

        throw new ConflictException("Bu sipariş işlemi daha zaten başlatılmış.");
    }
}