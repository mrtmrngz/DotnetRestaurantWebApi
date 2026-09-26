using AutoMapper;
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
using RestaurantApi.Application.Features.Rules.CategoryRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductCreateUpdateResponse>
{
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ISlugService _slugService;
    private readonly IProductRepository _productRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryRules _categoryRules;
    private readonly IUnitOfWork _uow;
    private readonly IMediaRepository _mediaRepository;
    private readonly IProductMediaRepository _productMediaRepository;

    public CreateProductCommandHandler(ILogger<CreateProductCommandHandler> logger, ICacheService cacheService,
        IMapper mapper, ISlugService slugService, IProductRepository productRepository, IFileStorage fileStorage,
        ICategoryRepository categoryRepository, CategoryRules categoryRules, IUnitOfWork uow,
        IMediaRepository mediaRepository, IProductMediaRepository productMediaRepository)
    {
        _logger = logger;
        _cacheService = cacheService;
        _mapper = mapper;
        _slugService = slugService;
        _productRepository = productRepository;
        _fileStorage = fileStorage;
        _categoryRepository = categoryRepository;
        _categoryRules = categoryRules;
        _uow = uow;
        _mediaRepository = mediaRepository;
        _productMediaRepository = productMediaRepository;
    }

    public async Task<ProductCreateUpdateResponse> Handle(CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ürün oluşturma işlemi başlıyor... Title:{Title}, CategoryId:{CatId}, Price: {Price}",
            request.Title, request.CategoryId, request.Price);

        await _categoryRules.ShouldCategoryEntityExist(
            await _categoryRepository.GetAllAsQueryable()
                .Where(ct => ct.Id == request.CategoryId)
                .FirstOrDefaultAsync(cancellationToken)
        );

        // handle file upload and generate slug
        var (fileResults, slug) = await UploadImagesAndCreateSlug(request.Title, request.Images, cancellationToken);

        // begin transaction
        await _uow.BeginTransaction(cancellationToken);

        Guid productId;

        try
        {
            // create entities
            productId = await SaveProductToDatabase(fileResults, request.CategoryId, request.Price, request.Description,
                request.Title, slug, cancellationToken);

            // commit
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.SafeDeleteMultipleFilesAsync(fileResults.Select(ur => ur.PublicId).ToList());
            throw;
        }
        
        // invalidate cache
        _logger.LogInformation("Ürünler redisten siliniyor...");
        await _cacheService.RemoveAsync(CacheKeys.AdminProducts());
        await _cacheService.RemoveAsync(CacheKeys.PublicProducts());
            
        _logger.LogInformation("Ürünler redisten silindi.");
            
        _logger.LogInformation("Ürün oluşturma işlemi başarılı bir şekilde gerçekleşti. Date:{Date}", DateTime.UtcNow);
            
        return new ProductCreateUpdateResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "Ürün başarılı bir şekilde oluşturuldu.",
            Id = productId
        };
    }

    #region Helper Methods

    private async Task<(List<UploadFileResult> fileResults, string slug)> UploadImagesAndCreateSlug(
        string title, IFormFileCollection fileCollection, CancellationToken ctx)
    {
        _logger.LogInformation("Slug oluşturulma aşaması başlıyor. Title:{Title}", title);
        
        IQueryable<Domain.Entities.Product> productQuery = _productRepository.GetAllAsQueryable();

        string slug = await _slugService.GenerateUniqueSlugAsync(productQuery, title, ctx);

        _logger.LogDebug("Ürün için slug üretildi. Title: {ProductTitle}, GeneratedSlug: {Slug}", title, slug);

        List<UploadFileResult> uploadResults = new List<UploadFileResult>();
        
        try
        {
            _logger.LogInformation("Ürün resimleri yüklemeye hazırlanıyor...");

            uploadResults = await _fileStorage.UploadMultipleAsync(fileCollection);

            _logger.LogInformation("Ürün resimleri başarılı bir şekilde yüklendi.");

            return (uploadResults, slug);
        }
        catch (Exception ex)
        {
            await _fileStorage.SafeDeleteMultipleFilesAsync(uploadResults.Select(ur => ur.PublicId).ToList());
            throw;
        }
    }

    private async Task<Guid> SaveProductToDatabase(
        List<UploadFileResult> fileResults,
        Guid categoryId,
        decimal price,
        string? desc,
        string title,
        string slug,
        CancellationToken ctx)
    {
        // create media
        _logger.LogInformation("Ürün resimleri media tablosuna kaydediliyor... Title:{Title}", title);
        
        List<Media> medias = _mapper.Map<List<Media>>(fileResults);

        _mediaRepository.AddRange(medias, ctx);
        
        _logger.LogInformation("Ürün resimleri media tablosuna kaydedildi. Title:{Title}", title);

        // create product

        _logger.LogInformation("Ürün oluşturuluyor... Title:{Title}", title);
        
        Product product = new Product
        {
            CategoryId = categoryId,
            Price = price,
            Description = desc,
            Title = title,
            Slug = slug,
        };

        _productRepository.Add(product, ctx);
        
        _logger.LogInformation("Ürün oluşturuldu. Title:{Title}", title);
        
        _logger.LogInformation("Ürün resimleri ile ürünün kendisi arasında bağlantı yapılıyor... Title:{Title}", title);

        // create product media
        List<ProductMedia> productMedias = medias
            .Select((media, index) => new ProductMedia
            {
                IsMain = index == 0,
                MediaId = media.Id,
                ProductId = product.Id,
                SortOrder = index,
            }).ToList();

        _productMediaRepository.AddRange(productMedias, ctx);
        
        _logger.LogInformation("Ürün resimleri ile ürünün kendisi arasında bağlantı yapıldı. Title:{Title}", title);
        
        _logger.LogInformation("Ürünler başarılı bir şekilde oluşturuldu. Id:{Id}", product.Id);

        return product.Id;
    }

    #endregion
}