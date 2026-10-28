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

    // Boş sepetle sipariş oluşturulamaz; domain koruması yerine iş kuralı hatası (422) döner.
    public void ShouldCartHaveItems(bool hasItems)
    {
        if (hasItems)
            return;

        _logger.LogWarning("Boş sepet için sipariş oluşturulamaz.");
        throw new UnprocessableEntityError("Sepetinizde sipariş oluşturulacak ürün bulunmuyor.");
    }
}