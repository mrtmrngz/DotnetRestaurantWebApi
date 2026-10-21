using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.CartItemRules;

public class CartItemRules
{
    private readonly ILogger<CartItemRules> _logger;

    public CartItemRules(ILogger<CartItemRules> logger)
    {
        _logger = logger;
    }

    public void CheckMaximumCartValue(int quantity)
    {
        if (quantity <= 100)
            return;
        
        _logger.LogInformation("Bir üründen sepete en fazla 100 adet eklenebilir.");

        throw new UnprocessableEntityError(
            "Bir üründen sepete en fazla 100 adet eklenebilir. Daha fazlası için restoran ile iletişime geçiniz.");
    }
}