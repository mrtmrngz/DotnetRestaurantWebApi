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
}