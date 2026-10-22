using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;

namespace RestaurantApi.Application.Features.Rules.CartRules;

public class CartRules
{
    private readonly ILogger<CartRules> _logger;

    public CartRules(ILogger<CartRules> logger)
    {
        _logger = logger;
    }

    public void ShouldCartExist(Domain.Entities.Cart? cart)
    {
        if(cart != null)
            return;
        
        _logger.LogInformation("Sepet bulunamadı.");
        throw new NotFoundException("Sepet bulunamadı.");
    }
}