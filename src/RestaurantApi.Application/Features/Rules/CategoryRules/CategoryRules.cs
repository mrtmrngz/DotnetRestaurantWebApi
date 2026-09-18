using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;

namespace RestaurantApi.Application.Features.Rules.CategoryRules;

public class CategoryRules
{
    private readonly ILogger<CategoryRules> _logger;

    public CategoryRules(ILogger<CategoryRules> logger)
    {
        _logger = logger;
    }


    public Task ShouldCategoryDetailExist(CategoryDetailQueryResult? category)
    {
        if (category is null)
        {
            _logger.LogWarning("Aranılan category bulunamadı.");
            throw new NotFoundException("Kategori bulunamadı.");
        }

        return Task.CompletedTask;
    }

    public Task ShouldCategoryEntityExist(Domain.Entities.Category? category)
    {
        if (category is null)
        {
            _logger.LogWarning("Aranılan category bulunamadı.");
            throw new NotFoundException("Kategori bulunamadı.");
        }

        return Task.CompletedTask;
    }

    public void ShouldBeValidForDeletion(Domain.Entities.Category? category)
    {
        if (category is null)
        {
            _logger.LogWarning("Aranılan category bulunamadı.");
            throw new NotFoundException("Kategori bulunamadı.");
        }

        if (category.IsDeleted)
        {
            _logger.LogWarning("Aranılan category silinmiş. CategoryId: {CategoryId}", category.Id);
            throw new NotFoundException("Kategori bulunamadı.");
        }
    }

    public void ShouldNotHasAnyActiveProduct(bool hasAny, Guid id)
    {
        if (hasAny)
        {
            _logger.LogWarning("İçerisinde aktif ürün bulunan kategori silinemez. CategoryId: {CategoryId}", id);
            throw new UnprocessableEntityError(
                "Bu kategoriye bağlı aktif ürünler bulunmaktadır. Lütfen önce ürünleri silin veya başka bir kategoriye taşıyın.");
        }
    }
}