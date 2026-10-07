using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ProductRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Commands.DeleteProductCommand;

public class DeleteProductCommandHandler: IRequestHandler<DeleteProductCommand, BaseResponse>
{
    private readonly ILogger<DeleteProductCommandHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly ProductRules _productRules;
    private readonly ICacheService _cacheService;

    public DeleteProductCommandHandler(ILogger<DeleteProductCommandHandler> logger, IProductRepository productRepository,
        ProductRules productRules, ICacheService cacheService)
    {
        _logger = logger;
        _productRepository = productRepository;
        _productRules = productRules;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ürün silme işlemi başlıyor... ProductId:{PrdId}", request.ProductId);

        // validate product for deletion (IgnoreQueryFilters => silinmiş ürünler de sorgulanır)

        var product = await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        _productRules.ShouldBeValidForDelete(product, request.ProductId);

        // soft delete product (ürün fotoğraflarına dokunulmaz)

        await _productRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .Where(p => p.Id == request.ProductId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(p => p.IsDeleted, true), cancellationToken);

        _logger.LogInformation("Ürün başarılı bir şekilde silindi (soft delete). ProductId:{PrdId}", request.ProductId);
        _logger.LogInformation("Ürünler redisten temizleniyor...");

        await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
        await _cacheService.RemoveAsync(CacheKeys.AdminProducts());
        await _cacheService.RemoveAsync(CacheKeys.PublicProductDetail(product!.Slug));
        await _cacheService.RemoveAsync(CacheKeys.AdminProductDetail(request.ProductId));

        _logger.LogInformation("Ürünler redisten temizlendi...");

        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Ürün başarılı bir şekilde silindi."
        };
    }
}
