using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.ProductRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;

public class PublicProductDetailQueryHandler: IRequestHandler<PublicProductDetailQuery, GeneralSuccessResponseWithData<PublicProductDetailQueryResult>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<PublicProductDetailQueryHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ProductRules _productRules;

    public PublicProductDetailQueryHandler(ICacheService cacheService, ILogger<PublicProductDetailQueryHandler> logger, IProductRepository productRepository, IMapper mapper, ProductRules productRules)
    {
        _cacheService = cacheService;
        _logger = logger;
        _productRepository = productRepository;
        _mapper = mapper;
        _productRules = productRules;
    }

    public async Task<GeneralSuccessResponseWithData<PublicProductDetailQueryResult>> Handle(PublicProductDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Public ürün detayı getiriliyor... Slug:{slug}", request.Slug);
        
        string cacheKey = CacheKeys.PublicProductDetail(request.Slug);
        PublicProductDetailQueryResult product = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            var prd = await _productRepository.GetAllAsQueryable()
                .Where(x => x.Slug == request.Slug)
                .AsNoTracking()
                .ProjectTo<PublicProductDetailQueryResult>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
            
            _productRules.ShouldPublicProductDetailExist(prd, request.Slug);

            return prd!;
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Public ürün detayı bulundu. Slug:{slug}", request.Slug);

        return new GeneralSuccessResponseWithData<PublicProductDetailQueryResult>(data: product);
    }
}