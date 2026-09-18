using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Category.Events.UpdateCategoryEvent;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Application.Features.Rules.CategoryRules;
using RestaurantApi.Application.Features.Rules.MediaRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Commands.UpdateCategoryCommand;

public class UpdateCategoryCommandHandler: IRequestHandler<UpdateCategoryCommand, BaseResponse>
{
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly CategoryRules _categoryRules;
    private readonly IFileStorage _fileStorage;
    private readonly ISlugService _slugService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaRepository _mediaRepository;
    private readonly MediaRules _mediaRules;
    private readonly IMediator _mediator;

    public UpdateCategoryCommandHandler(ILogger<UpdateCategoryCommandHandler> logger, ICategoryRepository categoryRepository, ICacheService cacheService, CategoryRules categoryRules, IFileStorage fileStorage, ISlugService slugService, IUnitOfWork unitOfWork, IMediaRepository mediaRepository, MediaRules mediaRules, IMediator mediator)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _categoryRules = categoryRules;
        _fileStorage = fileStorage;
        _slugService = slugService;
        _unitOfWork = unitOfWork;
        _mediaRepository = mediaRepository;
        _mediaRules = mediaRules;
        _mediator = mediator;
    }

    public async Task<BaseResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        // validate exist category
        var category = await ValidateCategory(request.CategoryId);
        
        // upload file and create slug
        var (uploadFileResult, slug) = await PrepeareUploadFileAndSlug(request.Image, request.Title, cancellationToken);
        
        try
        {
            // save category to db
            string? oldMediaPublicId = await SaveCategoryToDatabase(uploadFileResult, slug, request.Title, category, cancellationToken);
            
            // clear cache
            _logger.LogInformation("Kategoriler redisten temizleniyor.");
            await _cacheService.RemoveAsync(CacheKeys.Categories());
            
            // remove old file if new image uploaded.
            if (oldMediaPublicId is not null)
            {
                await _mediator.Publish(new UpdateCategoryEvent(oldMediaPublicId, category.Id));
            }
            
            return new BaseResponse()
            {
                Message = "Kategori başarılı bir şekilde güncellendi.",
                Code = Codes.CONTENT_UPDATED_SUCCESS
            };
        }
        catch (Exception ex)
        {
            if (uploadFileResult is not null)
            {
                _logger.LogError(ex,
                    "Kategori güncellenirken hata gerçekleşti! Yüklenen dosya temizleniyor. PublicId: {PublicId}, CategoryId: {CategoryId}",
                    uploadFileResult.PublicId, request.CategoryId);
                await SafeDeleteFileAsync(uploadFileResult.PublicId);
            }
            throw;
        }
    }

    #region Update Category Helper Methods

    private async Task<Domain.Entities.Category> ValidateCategory(Guid categoryId)
    {
        var category = await _categoryRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .Where(x => x.Id == categoryId)
            .FirstOrDefaultAsync();

        await _categoryRules.ShouldCategoryEntityExist(category);

        return category!;
    }

    private async Task<(UploadFileResult? UploadResult, string? Slug)> PrepeareUploadFileAndSlug(IFormFile? file,
        string? title, CancellationToken ctx)
    {
        UploadFileResult? uploadFileResult = null;
        string? slug = null;

        if (file is not null)
        {
            _logger.LogInformation("Görsel depolama servisine yükleniyor. FileName: {FileName}", file.FileName);
            uploadFileResult = await _fileStorage.UploadAsync(file);

            _logger.LogInformation("Görsel başarıyla yüklendi. PublicId: {PublicId}, Extension: {FileExtension}",
                uploadFileResult.PublicId, uploadFileResult.FileExtension);
        }

        if (title is not null)
        {
            try
            {
                IQueryable<Domain.Entities.Category> categoryQuery = _categoryRepository.GetAllAsQueryable();

                slug = await _slugService.GenerateUniqueSlugAsync(categoryQuery, title, ctx);
                _logger.LogDebug("Kategori için slug üretildi. Title: {CategoryTitle}, GeneratedSlug: {Slug}", title, slug);
            }
            catch
            {
                // Slug üretilirken patlarsa S3'te yetim dosya kalmaması için koruma!
                if (uploadFileResult is not null)
                {
                    await SafeDeleteFileAsync(uploadFileResult.PublicId);
                }
                throw;
            }
        }
        
        return (uploadFileResult, slug);
    }

    private async Task<string?> SaveCategoryToDatabase(UploadFileResult? fileResult, string? slug, string? title, Domain.Entities.Category category, CancellationToken ctx)
    {
        _logger.LogInformation("Kategori güncelleme için transaction başlatılıyor. CategoryId:{CatId}", category.Id);
        await _unitOfWork.BeginTransaction(ctx);

        string? oldMediaPublicId = null;

        if (fileResult is not null)
        {
            _logger.LogInformation("Kategori resmi güncelleniyor. CategoryId:{CatId}, MediaId: {MId}", category.Id, category.MediaId);
            var media = await _mediaRepository.GetMedia(category.MediaId, ctx);

            oldMediaPublicId = media!.PublicId;

            await _mediaRules.ShouldMediaExist(media);

            media!.PublicId = fileResult.PublicId;
            media!.Url = fileResult.Url;
            media!.FileExtension = fileResult.FileExtension;
            media!.FileType = fileResult.FileType;
            media!.Size = fileResult.Size;
            _logger.LogInformation("Kategori resmi güncellendi. CategoryId:{CatId}, MediaId: {MId}", category.Id, category.MediaId);
        }
        
        // handle title and slug

        if (title != null && slug != null)
        {
            _logger.LogInformation("Kategori başlığı güncelleniyor: CategoryId:{CatId}, OldTitle:{CatTitle} OldSlug:{categorySlug}",
                category.Id, category.Title, category.Slug);
            category.Title = title;
            category.Slug = slug;
            _logger.LogInformation("Kategori başlığı güncellendi: CategoryId:{CatId}, NewTitle:{title} NewSlug:{slug}",
                category.Id, title, slug);
        }
        
        await _unitOfWork.CommitTransactionAsync(ctx);
        
        _logger.LogInformation("Kategori güncellenme işlemi tamamlandı. CategoryId:{id}", category.Id);

        return oldMediaPublicId;
    }
    
    private async Task SafeDeleteFileAsync(string publicId)
    {
        try
        {
            await _fileStorage.DeleteAsync(publicId);
            _logger.LogInformation("Hata sonrası dosya depolama servisinden başarıyla silindi. PublicId: {PublicId}",
                publicId);
        }
        catch (Exception deleteEx)
        {
            _logger.LogCritical(deleteEx,
                "KRİTİK HATA: Veritabanı veya işlem hatası sonrası yüklenen dosya depolama servisinden SİLİNEMEDİ! Yetim dosya kaldı. PublicId: {PublicId}",
                publicId);
        }
    }

    #endregion
}