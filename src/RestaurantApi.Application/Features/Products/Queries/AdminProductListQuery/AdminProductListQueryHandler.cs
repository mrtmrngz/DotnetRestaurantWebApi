using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;

public class AdminProductListQueryHandler: IRequestHandler<AdminProductListQuery, GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<AdminProductListQueryHandler> _logger;
    private readonly IMapper _mapper;

    public AdminProductListQueryHandler(ICacheService cacheService, IProductRepository productRepository, ILogger<AdminProductListQueryHandler> logger, IMapper mapper)
    {
        _cacheService = cacheService;
        _productRepository = productRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>> Handle(AdminProductListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin ürün listesi getiriliyor...");
        
        string cacheKey = CacheKeys.AdminProducts();
        IReadOnlyList<AdminProductListQueryResult> products = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            return await _productRepository.GetAllAsQueryable()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .ProjectTo<AdminProductListQueryResult>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Admin ürün listesi bulundu. Count:{Count}", products.Count());

        return new GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>(data: products);
    }
}