using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Rules.ProductRules;

public class ProductRules
{
    private readonly ILogger<ProductRules> _logger;

    public ProductRules(ILogger<ProductRules> logger)
    {
        _logger = logger;
    }

    public void ValidateExistProductEntity(Product? product, Guid prdId)
    {
        if (product is null)
        {
            _logger.LogWarning("Aranılan ürün veritabanında bulunamadı. ProductId:{PrdId}", prdId);
            throw new NotFoundException("Ürün bulunamadı.");
        }
    }

    public void ShouldPublicProductDetailExist(PublicProductDetailQueryResult? result, string slug)
    {
        if (result is null)
        {
            _logger.LogWarning("Aranılan ürün veritabanında bulunamadı. Slug:{Slug}", slug);
            throw new NotFoundException("Ürün bulunamadı.");
        }
    }
}