using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, BaseResponse>
{
    private readonly ILogger<CreateCategoryCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMediaRepository _mediaRepository;
    private readonly ISlugService _slugService;

    public CreateCategoryCommandHandler(ILogger<CreateCategoryCommandHandler> logger, ICacheService cacheService,
        IUnitOfWork unitOfWork, IFileStorage fileStorage, ICategoryRepository categoryRepository,
        IMediaRepository mediaRepository, ISlugService slugService)
    {
        _logger = logger;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _categoryRepository = categoryRepository;
        _mediaRepository = mediaRepository;
        _slugService = slugService;
    }

    public async Task<BaseResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kategori oluşturma işlemi başlatıldı. Title: {CategoryTitle}", request.Title);

        // 1. Dosya Yükleme & Slug Hazırlığı
        var (uploadResult, slug) = await PrepareFileAndSlugAsync(request.Title, request.Image, cancellationToken);

        try
        {
            // 2. Veritabanı Kayıt İşlemleri (Transaction Yönetimi Tek Yerde)
            await SaveCategoryToDatabaseAsync(uploadResult, request.Title, slug, cancellationToken);

            // 3. Önbellek Temizliği
            await _cacheService.RemoveAsync(CacheKeys.Categories());
            _logger.LogInformation("Kategori önbelleği (cache) temizlendi. CacheKey: {CacheKey}",
                CacheKeys.Categories());
            
            await _cacheService.RemoveAsync(CacheKeys.AdminCategories());
            _logger.LogInformation("Admin Kategori önbelleği (cache) temizlendi. CacheKey: {CacheKey}",
                CacheKeys.AdminCategories());

            _logger.LogInformation(
                "Kategori oluşturma işlemi başarıyla tamamlandı. CategoryTitle: {CategoryTitle}, Slug: {Slug}",
                request.Title, slug);

            return new BaseResponse
            {
                Message = "Kategori başarılı bir şekilde oluşturuldu.",
                Code = Codes.CONTENT_CREATED_SUCCESS
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Kategori oluşturulurken hata gerçekleşti! Yüklenen dosya temizleniyor. PublicId: {PublicId}, CategoryTitle: {CategoryTitle}",
                uploadResult.PublicId, request.Title);

            await SafeDeleteFileAsync(uploadResult.PublicId);
            throw;
        }
    }

    #region Helper Methods

    private async Task<(UploadFileResult UploadResult, string Slug)> PrepareFileAndSlugAsync(string title,
        IFormFile file, CancellationToken ctx)
    {
        _logger.LogInformation("Görsel depolama servisine yükleniyor. FileName: {FileName}", file.FileName);
        UploadFileResult uploadResult = await _fileStorage.UploadAsync(file);

        _logger.LogInformation("Görsel başarıyla yüklendi. PublicId: {PublicId}, Extension: {FileExtension}",
            uploadResult.PublicId, uploadResult.FileExtension);

        try
        {
            IQueryable<Domain.Entities.Category> categoryQuery = _categoryRepository.GetAllAsQueryable();

            string slug = await _slugService.GenerateUniqueSlugAsync(categoryQuery, title, ctx);
            _logger.LogDebug("Kategori için slug üretildi. Title: {CategoryTitle}, GeneratedSlug: {Slug}", title, slug);

            return (uploadResult, slug);
        }
        catch
        {
            // Slug üretilirken patlarsa S3'te yetim dosya kalmaması için koruma!
            await SafeDeleteFileAsync(uploadResult.PublicId);
            throw;
        }
    }

    private async Task SaveCategoryToDatabaseAsync(UploadFileResult uploadResult, string title, string slug,
        CancellationToken ctx)
    {
        await _unitOfWork.BeginTransaction(ctx);
        _logger.LogDebug("Veritabanı transaction'ı başlatıldı. CategoryTitle: {CategoryTitle}", title);

        Media media = new Media
        {
            PublicId = uploadResult.PublicId,
            FileExtension = uploadResult.FileExtension,
            FileType = uploadResult.FileType,
            Size = uploadResult.Size,
            Url = uploadResult.Url
        };

        _mediaRepository.CreateMedia(media, ctx);

        Domain.Entities.Category cat = new Domain.Entities.Category
        {
            Title = title,
            MediaId = media.Id,
            IsDeleted = false,
            Slug = slug,
        };

        _categoryRepository.CreateCategory(cat, ctx);

        await _unitOfWork.CommitTransactionAsync(ctx);
        _logger.LogInformation(
            "Kategori ve Medya veritabanına başarıyla kaydedildi. CategoryId: {CategoryId}, MediaId: {MediaId}", cat.Id,
            media.Id);
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