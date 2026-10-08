using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Rules.DiscountRules;

public class DiscountRules
{
    private readonly ILogger<DiscountRules> _logger;
    private readonly ProductRules.ProductRules _productRules;

    public DiscountRules(ILogger<DiscountRules> logger, ProductRules.ProductRules productRules)
    {
        _logger = logger;
        _productRules = productRules;
    }

    // İndirim oluşturulacak ürün yoksa veya soft-delete edilmişse indirim oluşturulamaz.
    public void ShouldProductBeEligibleForDiscount(Product? product, Guid productId)
    {
        // Ürün yoksa "Ürün bulunamadı." ile NotFoundException fırlatır (ProductRules).
        _productRules.ValidateExistProductEntity(product, productId);

        if (product!.IsDeleted)
        {
            _logger.LogWarning("İndirim oluşturulmak istenen ürün silinmiş durumda. ProductId:{PrdId}", productId);
            throw new NotFoundException("Ürün bulunamadı.");
        }
    }

    // Bir ürünün yalnızca bir adet indirimi olabilir.
    public void ShouldProductNotHaveDiscount(bool hasDiscount, Guid productId)
    {
        if (hasDiscount)
        {
            _logger.LogWarning("İndirim oluşturulmak istenen ürünün zaten bir indirimi bulunuyor. ProductId:{PrdId}",
                productId);
            throw new ConflictException("Bu ürünün zaten bir indirimi bulunmaktadır.");
        }
    }

    // Başlangıç ve bitiş tarihleri bugünden önce olamaz; bitiş, başlangıçtan sonra olmalıdır.
    public void ShouldDiscountDatesBeValid(DateTime startDate, DateTime endDate, DateTime now)
    {
        if (startDate.Date < now.Date)
        {
            _logger.LogWarning("İndirim başlangıç tarihi geçmiş bir tarih. StartDate:{StartDate}", startDate);
            throw new UnprocessableEntityError("İndirim başlangıç tarihi bugünden önce olamaz.");
        }

        if (endDate.Date < now.Date)
        {
            _logger.LogWarning("İndirim bitiş tarihi geçmiş bir tarih. EndDate:{EndDate}", endDate);
            throw new UnprocessableEntityError("İndirim bitiş tarihi bugünden önce olamaz.");
        }

        if (endDate <= startDate)
        {
            _logger.LogWarning("İndirim bitiş tarihi başlangıçtan önce veya eşit. StartDate:{StartDate}, EndDate:{EndDate}",
                startDate, endDate);
            throw new UnprocessableEntityError("İndirim bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        }
    }

    // İndirimin şu anki zaman aralığında olup olmadığını (IsActive) hesaplar.
    public bool CalculateIsActive(DateTime startDate, DateTime endDate, DateTime now)
    {
        return startDate <= now && now <= endDate;
    }
}
