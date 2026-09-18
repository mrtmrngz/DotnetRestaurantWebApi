using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.CategoryRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Commands.DeleteCategoryCommand;

public class DeleteCategoryCommandHandler: IRequestHandler<DeleteCategoryCommand, BaseResponse>
{
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryRules _categoryRules;
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;

    public DeleteCategoryCommandHandler(ILogger<DeleteCategoryCommandHandler> logger, ICategoryRepository categoryRepository, CategoryRules categoryRules, ICacheService cacheService, IProductRepository productRepository)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _categoryRules = categoryRules;
        _cacheService = cacheService;
        _productRepository = productRepository;
    }

    public async Task<BaseResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kategori silme işlemi başlıyor... CategoryId:{id}", request.CategoryId);

        // validate category for deletion

        await ValidateCategoryForDeletion(request.CategoryId, cancellationToken);
        
        // delete category
        await _categoryRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .Where(c => c.Id == request.CategoryId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(c => c.IsDeleted, true), cancellationToken);
        
        _logger.LogInformation("Kategori başarılı bir şekilde silindi. CategoryId:{id}", request.CategoryId);
        _logger.LogInformation("Kategoriler redisten temizleniyor...");

        await _cacheService.RemoveAsync(CacheKeys.Categories());
        await _cacheService.RemoveAsync(CacheKeys.AdminCategories());
        
        _logger.LogInformation("Kategoriler redisten temizlendi...");
        
        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Kategori başarılı bir şekilde silindi."
        };
    }

    #region MyRegion

    private async Task ValidateCategoryForDeletion(Guid categoryId, CancellationToken ctx)
    {
        var category = await _categoryRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ctx);
        
        _categoryRules.ShouldBeValidForDeletion(category);

        var hasActiveProduct = await _productRepository.GetAllAsQueryable()
            .AnyAsync(p => p.CategoryId == categoryId, ctx);
        
        _categoryRules.ShouldNotHasAnyActiveProduct(hasActiveProduct, categoryId);
    }

    #endregion
}