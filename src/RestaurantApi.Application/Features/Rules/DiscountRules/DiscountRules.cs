using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Enums;

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

    public void ShouldProductBeEligibleForDiscount(Product? product, Guid productId)
    {
        _productRules.ValidateExistProductEntity(product, productId);

        if (product!.IsDeleted)
        {
            _logger.LogWarning("İndirim oluşturulmak istenen ürün silinmiş durumda. ProductId:{PrdId}", productId);
            throw new NotFoundException("Ürün bulunamadı.");
        }
    }

    public void ShouldProductNotHaveDiscount(bool hasDiscount, Guid productId)
    {
        if (hasDiscount)
        {
            _logger.LogWarning("İndirim oluşturulmak istenen ürünün zaten bir indirimi bulunuyor. ProductId:{PrdId}",
                productId);
            throw new ConflictException("Bu ürünün zaten bir indirimi bulunmaktadır.");
        }
    }

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

    public bool CalculateIsActive(DateTime startDate, DateTime endDate, DateTime now)
    {
        return startDate <= now && now <= endDate;
    }

    public void ShouldDiscountExist(GetDiscountDetailQueryResult? result, Guid discountId)
    {
        if (result is null)
        {
            _logger.LogWarning("Aranılan indirim bulunamadı. DiscountId:{DiscountId}", discountId);
            throw new NotFoundException("İndirim bulunamadı.");
        }
    }

    public void ShouldDiscountExist(Domain.Entities.Discount? discount, Guid discountId)
    {
        if (discount is null)
        {
            _logger.LogWarning("Aranılan indirim bulunamadı. DiscountId:{DiscountId}", discountId);
            throw new NotFoundException("İndirim bulunamadı.");
        }
    }

    public void ShouldDiscountNotBeEnded(DateTime endDate, DateTime now)
    {
        if (now > endDate)
        {
            _logger.LogWarning("Bitmiş indirim güncellenemez. EndDate:{EndDate}", endDate);
            throw new ConflictException("Bitmiş indirim güncellenemez.");
        }
    }

    public void ShouldDiscountNotBeStarted(DateTime startDate, DateTime now)
    {
        if (now >= startDate)
        {
            _logger.LogWarning("Başlamış indirim güncellenemez. StartDate:{StartDate}", startDate);
            throw new ConflictException("Başlamış indirim güncellenemez.");
        }
    }

    public DiscountStatus ResolveStatus(DateTime startDate, DateTime endDate, DateTime now)
    {
        if (now < startDate)
            return DiscountStatus.Upcoming;

        if (now > endDate)
            return DiscountStatus.Passive;

        return DiscountStatus.Active;
    }
}
