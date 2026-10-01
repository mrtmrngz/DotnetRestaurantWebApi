using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Files.Dtos;
using RestaurantApi.Application.Features.Products.Events.UpdateProductEvent;
using RestaurantApi.Application.Features.Rules.CategoryRules;
using RestaurantApi.Application.Features.Rules.ProductMediaRules;
using RestaurantApi.Application.Features.Rules.ProductRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Products.Commands.UpdateProductCommand;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductCreateUpdateResponse>
{
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ProductRules _productRules;
    private readonly CategoryRules _categoryRules;
    private readonly IFileStorage _fileStorage;
    private readonly ISlugService _slugService;
    private readonly IMediaRepository _mediaRepository;
    private readonly IProductMediaRepository _productMediaRepository;
    private readonly ProductMediaRules _productMediaRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;

    public UpdateProductCommandHandler(ILogger<UpdateProductCommandHandler> logger,
        ICategoryRepository categoryRepository, IProductRepository productRepository, ProductRules productRules,
        CategoryRules categoryRules, IFileStorage fileStorage, ISlugService slugService,
        IMediaRepository mediaRepository, IProductMediaRepository productMediaRepository,
        ProductMediaRules productMediaRules, IUnitOfWork uow, ICacheService cacheService, IMediator mediator)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _productRules = productRules;
        _categoryRules = categoryRules;
        _fileStorage = fileStorage;
        _slugService = slugService;
        _mediaRepository = mediaRepository;
        _productMediaRepository = productMediaRepository;
        _productMediaRules = productMediaRules;
        _uow = uow;
        _cacheService = cacheService;
        _mediator = mediator;
    }

    public async Task<ProductCreateUpdateResponse> Handle(UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ürün güncelleme işlemi başlıyor... ProductId:{PrdId}, CategoryId:{CatId}, Price:{Price}",
            request.ProductId, request.CategoryId, request.Price);

        // validate product and if category exist

        var product = await ValidateProductAndCategory(request.ProductId, request.CategoryId, cancellationToken);

        List<UploadFileResult> fileResult = new List<UploadFileResult>();
        string? slug = null;

        // handle new slug if title exist

        if (request.Title is not null && request.Title != product.Title)
        {
            _logger.LogInformation(
                "Ürün başlığı değişti, yeni slug üretilecek. ProductId:{PrdId}, OldTitle:{OldTitle}, NewTitle:{NewTitle}",
                request.ProductId, product.Title, request.Title);

            slug = await HandleGenerateNewSlug(request.Title, cancellationToken);
        }

        // handle new images

        if (request.Images is not null && request.Images.Count > 0)
        {
            _logger.LogInformation("Yeni ürün görselleri yüklenecek. ProductId:{PrdId}, Adet:{Count}",
                request.ProductId, request.Images.Count);

            fileResult = await UploadNewImages(request.Images);
        }

        // validate product image

        if (request.DeletedImagePublicIds is not null && request.DeletedImagePublicIds.Count > 0)
        {
            var nwImageCount = request.Images != null ? request.Images.Count : 0;
            await ValidateProductImage(request.DeletedImagePublicIds, request.ProductId, nwImageCount,
                cancellationToken);
        }

        try
        {
            _logger.LogInformation("Ürün güncelleme için transaction başlatılıyor. ProductId:{PrdId}", request.ProductId);

            await _uow.BeginTransaction(cancellationToken);
            
            // remove old images   
            if (request.DeletedImagePublicIds != null && request.DeletedImagePublicIds.Count > 0)
            {
                await DeleteOldImagesAndNewOrder(request.DeletedImagePublicIds, request.ProductId, cancellationToken);
            }

            if (fileResult.Count > 0)
            {
                await SaveDatabaseToImage(fileResult, request.ProductId, cancellationToken);
            }
            
            UpdateProductEntity(product, request, slug);

            _logger.LogInformation("Ürün güncelleme değişiklikleri commit ediliyor. ProductId:{PrdId}", request.ProductId);

            await _uow.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation("Ürün başarılı bir şekilde güncellendi: Id: {PrdID}", request.ProductId);
            
            _logger.LogInformation("Ürünler redisten temizleniyor...");

            await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
            await _cacheService.RemoveAsync(CacheKeys.AdminProducts());
            await _cacheService.RemoveAsync(CacheKeys.AdminProductDetail(request.ProductId));
            await _cacheService.RemoveAsync(CacheKeys.PublicProductDetail(request.ProductId));
            
            _logger.LogInformation("Ürünler redisten silindi...");
            
            _logger.LogInformation("Ürün güncelleneme işlemi tamamlandı...");
            
            // handle old image delete

            if (request.DeletedImagePublicIds is not null && request.DeletedImagePublicIds.Count > 0)
            {
                _logger.LogInformation(
                    "Eski ürün görsellerinin depolamadan silinmesi için event yayınlanıyor. ProductId:{PrdId}, PublicIdSayısı:{Count}",
                    request.ProductId, request.DeletedImagePublicIds.Count);

                await _mediator.Publish(new UpdateProductEvent(request.DeletedImagePublicIds));
            }

            return new ProductCreateUpdateResponse()
            {
                Id = request.ProductId,
                Code = Codes.CONTENT_UPDATED_SUCCESS,
                Message = "Success"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ürün güncellenirken hata gerçekleşti! Yüklenen dosyalar temizleniyor. ProductId:{PrdId}",
                request.ProductId);

            await _fileStorage.SafeDeleteMultipleFilesAsync(fileResult.Select(fr => fr.PublicId).ToList());
            throw;
        }
    }

    #region Helper Methods

    private async Task<Product> ValidateProductAndCategory(Guid productId, Guid? categoryId, CancellationToken ctx)
    {
        _logger.LogInformation("Ürün ve kategori doğrulaması yapılıyor. ProductId:{PrdId}, CategoryId:{CatId}",
            productId, categoryId);

        var product = await _productRepository.GetAllAsQueryable().FirstOrDefaultAsync(x => x.Id == productId, ctx);

        _productRules.ValidateExistProductEntity(product, productId);

        if (categoryId.HasValue && categoryId != product!.CategoryId)
        {
            _logger.LogInformation(
                "Ürünün yeni kategorisi doğrulanıyor. ProductId:{PrdId}, NewCategoryId:{CatId}",
                productId, categoryId);

            await _categoryRules.ShouldCategoryEntityExist(
                await _categoryRepository.GetAllAsQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == categoryId, ctx)
            );

            _logger.LogDebug("Yeni kategori doğrulaması başarılı. NewCategoryId:{CatId}", categoryId);
        }

        _logger.LogInformation("Ürün ve kategori doğrulaması başarılı. ProductId:{PrdId}", productId);

        return product!;
    }

    private async Task<string> HandleGenerateNewSlug(string title, CancellationToken ctx)
    {
        _logger.LogInformation("Slug oluşturulma aşaması başlıyor. Title:{Title}", title);

        IQueryable<Product> productQuery = _productRepository.GetAllAsQueryable();

        string slug = await _slugService.GenerateUniqueSlugAsync(productQuery, title, ctx);

        _logger.LogDebug("Ürün için slug üretildi. Title: {ProductTitle}, GeneratedSlug: {Slug}", title, slug);

        return slug;
    }

    private async Task<List<UploadFileResult>> UploadNewImages(IFormFileCollection files)
    {
        List<UploadFileResult>? fileResult = new List<UploadFileResult>();

        try
        {
            _logger.LogInformation("Ürün resimleri yüklemeye hazırlanıyor...");

            fileResult = await _fileStorage.UploadMultipleAsync(files);

            _logger.LogInformation("Ürün resimleri başarılı bir şekilde yüklendi.");

            return fileResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürün resimleri yüklenirken hata gerçekleşti! Yüklenen dosyalar temizleniyor.");

            await _fileStorage.SafeDeleteMultipleFilesAsync(fileResult.Select(ur => ur.PublicId).ToList());
            throw;
        }
    }

    private async Task ValidateProductImage(IReadOnlyList<string> deletedPublicIds, Guid productId, int newImageCount,
        CancellationToken ctx)
    {
        _logger.LogInformation(
            "Ürün görselleri doğrulanıyor. ProductId:{PrdId}, SilinecekGörselSayısı:{DeleteCount}, YeniGörselSayısı:{NewCount}",
            productId, deletedPublicIds.Count, newImageCount);

        var productImageCount = await _productMediaRepository.GetAllAsQueryable()
            .CountAsync(pm => pm.ProductId == productId, ctx);
        
        var deletedImageCount = await _productMediaRepository
            .GetAllAsQueryable()
            .CountAsync(
                pm => pm.ProductId == productId &&
                      deletedPublicIds.Contains(pm.Media.PublicId),
                ctx);
        
        var finalImageCount =
            productImageCount
            - deletedPublicIds.Count
            + newImageCount;

        _logger.LogDebug(
            "Ürün görsel sayıları hesaplandı. ProductId:{PrdId}, Mevcut:{CurrentCount}, Silinen:{DeletedCount}, Yeni:{NewCount}, GüncelSonrası:{FinalCount}",
            productId, productImageCount, deletedImageCount, newImageCount, finalImageCount);

        _productMediaRules.ShouldDeletedImgCountMatchesPrdDeletedImgCount(deletedImageCount, deletedPublicIds.Count);
        
        _productMediaRules.ShouldImageCountNotExceedMaximum(finalImageCount);

        _productMediaRules.ShouldDeletedImageCountCantEqualCurrentImageCount(newImageCount, deletedPublicIds.Count,
            productImageCount);

        _logger.LogInformation("Ürün görselleri doğrulaması başarılı. ProductId:{PrdId}", productId);
    }

    private async Task DeleteOldImagesAndNewOrder(IReadOnlyList<string> publicIds, Guid prdId, CancellationToken ctx)
    {
        _logger.LogInformation("Eski ürün görselleri siliniyor. ProductId:{PrdId}, PublicIdSayısı:{Count}",
            prdId, publicIds.Count);

        var mediasToDelete = await _mediaRepository.GetAllAsQueryable()
            .Where(m => publicIds.Contains(m.PublicId))
            .ToListAsync(ctx);

        if (!mediasToDelete.Any())
        {
            _logger.LogWarning("Silinecek ürün görseli bulunamadı. ProductId:{PrdId}", prdId);
            return;
        }

        _mediaRepository.DeleteMultipleMedia(mediasToDelete);

        await _uow.SaveChangesAsync(ctx);

        _logger.LogInformation("Eski ürün görselleri veritabanından silindi. ProductId:{PrdId}, SilinenSayı:{Count}",
            prdId, mediasToDelete.Count);

        List<ProductMedia> remainingPMs = await _productMediaRepository.GetAllAsQueryable()
            .Where(pm => pm.ProductId == prdId)
            .OrderBy(pm => pm.SortOrder)
            .ToListAsync(ctx);

        if (remainingPMs.Any())
        {
            bool hasMain = remainingPMs.Any(pm => pm.IsMain);

            remainingPMs.Select((img, index) =>
            {
                img.SortOrder = index;
                img.IsMain = hasMain ? img.IsMain : index == 0;
                return img;
            }).ToList();

            _logger.LogInformation("Kalan ürün görselleri yeniden sıralandı. ProductId:{PrdId}, KalanSayı:{Count}",
                prdId, remainingPMs.Count);
        }

        await _uow.SaveChangesAsync(ctx);
    }

    private async Task SaveDatabaseToImage(List<UploadFileResult> fileResults, Guid prdId,
        CancellationToken ctx)
    {
        _logger.LogInformation("Yeni ürün görselleri veritabanına kaydediliyor. ProductId:{PrdId}, GörselSayısı:{Count}",
            prdId, fileResults.Count);

        List<Media> newMedias = fileResults.Select(result => new Media
        {
            PublicId = result.PublicId,
            FileExtension = result.FileExtension,
            FileType = result.FileType,
            Size = result.Size,
            Url = result.Url
        }).ToList();

        _mediaRepository.AddRange(newMedias, ctx);

        var oldPms = await _productMediaRepository.GetAllAsQueryable()
            .Where(pm => pm.ProductId == prdId)
            .ToListAsync(ctx);

        bool hasMain = oldPms.Any(pm => pm.IsMain);
        ProductMedia? lastPm = oldPms.OrderByDescending(pm => pm.SortOrder).FirstOrDefault();

        List<ProductMedia> newPms = newMedias.Select((media, index) => new ProductMedia
        {
            IsMain = !hasMain && index == 0,
            SortOrder = lastPm != null ? lastPm.SortOrder + index + 1 : index,
            ProductId = prdId,
            MediaId = media.Id
        }).ToList();

        _productMediaRepository.AddRange(newPms, ctx);

        _logger.LogInformation(
            "Yeni ürün görselleri ve ürün-görsel bağlantıları kaydedildi. ProductId:{PrdId}, YeniGörselSayısı:{Count}",
            prdId, newPms.Count);
    }

    private void UpdateProductEntity(Product product, UpdateProductCommand request, string? slug)
    {
        _logger.LogInformation(
            "Ürün alanları güncelleniyor. ProductId:{PrdId}, OldTitle:{OldTitle}, NewTitle:{NewTitle}, OldPrice:{OldPrice}, NewPrice:{NewPrice}, OldCategoryId:{OldCatId}, NewCategoryId:{NewCatId}",
            product.Id, product.Title, request.Title, product.Price, request.Price, product.CategoryId,
            request.CategoryId);

        product.Title = request.Title ?? product.Title;
        product.Slug = slug ?? product.Slug;
        product.Description = request.Description ?? product.Description;
        product.Price = request.Price ?? product.Price;
        product.CategoryId = request.CategoryId ?? product.CategoryId;

        _logger.LogInformation(
            "Ürün alanları güncellendi. ProductId:{PrdId}, Title:{Title}, Slug:{Slug}, Price:{Price}, CategoryId:{CatId}",
            product.Id, product.Title, product.Slug, product.Price, product.CategoryId);
    }

    #endregion
}