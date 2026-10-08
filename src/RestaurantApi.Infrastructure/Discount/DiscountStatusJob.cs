using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;

namespace RestaurantApi.Infrastructure.Discount;

// Belirli bir tarihte çalışacak şekilde zamanlanan indirim durum işleri.
public class DiscountStatusJob
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DiscountStatusJob> _logger;

    public DiscountStatusJob(IDiscountRepository discountRepository, IProductRepository productRepository,
        IUnitOfWork unitOfWork, ICacheService cacheService, ILogger<DiscountStatusJob> logger)
    {
        _discountRepository = discountRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task ActivateAsync(Guid discountId)
    {
        return SetStatusAsync(discountId, isActive: true);
    }

    public Task DeactivateAsync(Guid discountId)
    {
        return SetStatusAsync(discountId, isActive: false);
    }

    private async Task SetStatusAsync(Guid discountId, bool isActive)
    {
        var discount = await _discountRepository.GetAllAsQueryable()
            .FirstOrDefaultAsync(d => d.Id == discountId);

        if (discount is null)
        {
            _logger.LogWarning("İndirim bulunamadı. DiscountId:{DiscountId}", discountId);
            return;
        }

        discount.IsActive = isActive;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            isActive
                ? "İndirim başladı. DiscountId:{DiscountId}, ProductId:{PrdId}"
                : "İndirim sona erdi. DiscountId:{DiscountId}, ProductId:{PrdId}",
            discount.Id, discount.ProductId);

        // indirim durumu ürün listesi/detayını etkilediği için ilgili cache'ler temizlenir

        await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
        await _cacheService.RemoveAsync(CacheKeys.AdminProducts());

        var product = await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == discount.ProductId);

        if (product is not null)
        {
            await _cacheService.RemoveAsync(CacheKeys.PublicProductDetail(product.Slug));
            await _cacheService.RemoveAsync(CacheKeys.AdminProductDetail(product.Id));
        }
    }
}
