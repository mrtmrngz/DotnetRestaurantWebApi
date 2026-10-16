using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.DiscountRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Discount.Commands.UpdateDiscountCommand;

public class UpdateDiscountCommandHandler: IRequestHandler<UpdateDiscountCommand, BaseResponse>
{
    private readonly ILogger<UpdateDiscountCommandHandler> _logger;
    private readonly IDiscountRepository _discountRepository;
    private readonly IProductRepository _productRepository;
    private readonly DiscountRules _discountRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;

    public UpdateDiscountCommandHandler(ILogger<UpdateDiscountCommandHandler> logger,
        IDiscountRepository discountRepository, IProductRepository productRepository, DiscountRules discountRules,
        IUnitOfWork uow, ICacheService cacheService)
    {
        _logger = logger;
        _discountRepository = discountRepository;
        _productRepository = productRepository;
        _discountRules = discountRules;
        _uow = uow;
        _cacheService = cacheService;
    }

    #region Handle

    public async Task<BaseResponse> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim güncelleme işlemi başlıyor... DiscountId:{DiscountId}", request.DiscountId);

        var discount = await ValidateDiscount(request.DiscountId, cancellationToken);

        var now = DateTime.UtcNow;

        _discountRules.ShouldDiscountNotBeEnded(discount.EndDate, now);

        // Kural 4
        if (request.StartDate.HasValue)
            _discountRules.ShouldDiscountNotBeStarted(discount.StartDate, now);

        var oldProduct = discount.Product;

        // Kural 7
        var newProduct = await ValidateNewProduct(discount, request.ProductId, now, cancellationToken);

        UpdateDiscountFields(discount, request, newProduct);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("İndirim başarılı bir şekilde güncellendi. DiscountId:{DiscountId}", discount.Id);

        await InvalidateCachesAsync(oldProduct);

        if (newProduct is not null)
            await InvalidateCachesAsync(newProduct);

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "İndirim başarılı bir şekilde güncellendi."
        };
    }

    #endregion

    #region Helper Methods

    private async Task<Domain.Entities.Discount> ValidateDiscount(Guid discountId, CancellationToken ctx)
    {
        var discount = await _discountRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == discountId, ctx);

        _discountRules.ShouldDiscountExist(discount, discountId);

        return discount!;
    }

    private async Task<Domain.Entities.Product?> ValidateNewProduct(Domain.Entities.Discount discount,
        Guid? productId, DateTime now, CancellationToken ctx)
    {
        if (productId is null || productId.Value == discount.ProductId)
            return null;
        
        _discountRules.ShouldDiscountNotBeStarted(discount.StartDate, now);

        var product = await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == productId.Value, ctx);

        _discountRules.ShouldProductBeEligibleForDiscount(product, productId.Value);

        // Kural 7.4
        var hasDiscount = await _discountRepository.GetAllAsQueryable()
            .AnyAsync(d => d.ProductId == productId.Value && d.Id != discount.Id && d.IsActive, ctx);

        _discountRules.ShouldProductNotHaveDiscount(hasDiscount, productId.Value);

        return product;
    }

    private static void UpdateDiscountFields(Domain.Entities.Discount discount, UpdateDiscountCommand request,
        Domain.Entities.Product? newProduct)
    {
        if (request.Rate.HasValue)
            discount.Rate = request.Rate.Value;

        if (request.StartDate.HasValue)
            discount.StartDate = request.StartDate.Value;

        if (request.EndDate.HasValue)
            discount.EndDate = request.EndDate.Value;

        if (request.IsActive.HasValue)
            discount.IsActive = request.IsActive.Value;

        if (newProduct is not null)
            discount.ProductId = newProduct.Id;
    }

    private async Task InvalidateCachesAsync(Domain.Entities.Product product)
    {
        await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
        await _cacheService.RemoveAsync(CacheKeys.AdminProducts());
        await _cacheService.RemoveAsync(CacheKeys.PublicProductDetail(product.Slug));
        await _cacheService.RemoveAsync(CacheKeys.AdminProductDetail(product.Id));
        await _cacheService.RemoveAsync(CacheKeys.AdminDiscounts());
    }

    #endregion
}
