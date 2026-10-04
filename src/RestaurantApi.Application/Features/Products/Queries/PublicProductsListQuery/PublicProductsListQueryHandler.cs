using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;

public class PublicProductsListQueryHandler: IRequestHandler<PublicProductsListQuery, GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<PublicProductsListQueryHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public PublicProductsListQueryHandler(ICacheService cacheService, ILogger<PublicProductsListQueryHandler> logger, IProductRepository productRepository, IMapper mapper)
    {
        _cacheService = cacheService;
        _logger = logger;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>> Handle(PublicProductsListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Public ürün listesi getiriliyor...");
        
        string cacheKey = CacheKeys.PublicProducts();
        IReadOnlyList<PublicProductsListQueryResult> products = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            return await _productRepository.GetAllAsQueryable()
                .AsNoTracking()
                .ProjectTo<PublicProductsListQueryResult>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Public ürün listesi bulundu. Count:{Count}", products.Count());

        return new GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>(data: products);
    }
}