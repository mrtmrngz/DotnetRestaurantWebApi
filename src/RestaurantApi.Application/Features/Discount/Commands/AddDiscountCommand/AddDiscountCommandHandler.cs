using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.DiscountRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;

public class AddDiscountCommandHandler: IRequestHandler<AddDiscountCommand, BaseResponse>
{
    private readonly ILogger<AddDiscountCommandHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly DiscountRules _discountRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;
    private readonly IDiscountScheduler _discountScheduler;

    public AddDiscountCommandHandler(ILogger<AddDiscountCommandHandler> logger, IProductRepository productRepository,
        IDiscountRepository discountRepository, DiscountRules discountRules, IUnitOfWork uow, ICacheService cacheService,
        IDiscountScheduler discountScheduler)
    {
        _logger = logger;
        _productRepository = productRepository;
        _discountRepository = discountRepository;
        _discountRules = discountRules;
        _uow = uow;
        _cacheService = cacheService;
        _discountScheduler = discountScheduler;
    }

    #region Handle

    public async Task<BaseResponse> Handle(AddDiscountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim ekleme işlemi başlıyor... ProductId:{PrdId}, DiscountRate:{Rate}",
            request.ProductId, request.DiscountRate);

        // ürün doğrulaması (var mı, silinmiş mi, zaten indirimi var mı)

        var product = await ValidateProductForDiscountAsync(request.ProductId, cancellationToken);

        var now = DateTime.UtcNow;

        _discountRules.ShouldDiscountDatesBeValid(request.StartDate, request.EndDate, now);

        // indirim kaydı oluştur

        var discount = new Domain.Entities.Discount
        {
            ProductId = request.ProductId,
            Rate = request.DiscountRate,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = _discountRules.CalculateIsActive(request.StartDate, request.EndDate, now)
        };

        _discountRepository.AddDiscount(discount, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("İndirim başarılı bir şekilde eklendi. DiscountId:{DiscountId}, IsActive:{IsActive}",
            discount.Id, discount.IsActive);


        await InvalidateProductCachesAsync(product);
        ScheduleDiscountJobs(discount, now);

        return new BaseResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "İndirim başarılı bir şekilde eklendi."
        };
    }

    #endregion

    #region Helper Methods

    private async Task<Domain.Entities.Product> ValidateProductForDiscountAsync(Guid productId, CancellationToken ctx)
    {
        // IgnoreQueryFilters => soft-deleted ürün de tespit edilebilsin
        var product = await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == productId, ctx);

        _discountRules.ShouldProductBeEligibleForDiscount(product, productId);

        // her ürünün yalnızca bir indirimi olabilir

        var hasDiscount = await _discountRepository.GetAllAsQueryable()
            .AnyAsync(d => d.ProductId == productId, ctx);

        _discountRules.ShouldProductNotHaveDiscount(hasDiscount, productId);

        return product!;
    }

    private async Task InvalidateProductCachesAsync(Domain.Entities.Product product)
    {
        // indirim, ürün listesi/detayını etkilediği için ilgili cache'ler temizlenir
        await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
        await _cacheService.RemoveAsync(CacheKeys.AdminProducts());
        await _cacheService.RemoveAsync(CacheKeys.PublicProductDetail(product.Slug));
        await _cacheService.RemoveAsync(CacheKeys.AdminProductDetail(product.Id));
    }

    private void ScheduleDiscountJobs(Domain.Entities.Discount discount, DateTime now)
    {
        // indirim başlangıç/bitiş tarihlerinde çalışacak zamanlanmış işler

        if (discount.StartDate > now)
            _discountScheduler.ScheduleActivation(discount.Id, discount.StartDate);

        if (discount.EndDate > now)
            _discountScheduler.ScheduleDeactivation(discount.Id, discount.EndDate);
    }

    #endregion
}
