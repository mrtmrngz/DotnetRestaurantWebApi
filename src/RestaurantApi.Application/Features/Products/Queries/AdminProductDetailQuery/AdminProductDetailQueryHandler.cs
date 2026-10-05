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

namespace RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;

public class AdminProductDetailQueryHandler: IRequestHandler<AdminProductDetailQuery, GeneralSuccessResponseWithData<AdminProductDetailQueryResult>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminProductDetailQueryHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IMapper  _mapper;
    private readonly ProductRules _productRules;

    public AdminProductDetailQueryHandler(ICacheService cacheService, ILogger<AdminProductDetailQueryHandler> logger, IProductRepository productRepository, IMapper mapper, ProductRules productRules)
    {
        _cacheService = cacheService;
        _logger = logger;
        _productRepository = productRepository;
        _mapper = mapper;
        _productRules = productRules;
    }

    public async Task<GeneralSuccessResponseWithData<AdminProductDetailQueryResult>> Handle(AdminProductDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin ürün detayı getiriliyor... Id:{slug}", request.ProductId);
        
        string cacheKey = CacheKeys.AdminProductDetail(request.ProductId);
        AdminProductDetailQueryResult product = await _cacheService.GetOrInternalSetAsync(cacheKey, async () =>
        {
            var prd = await _productRepository.GetAllAsQueryable()
                .IgnoreQueryFilters()
                .Where(x => x.Id == request.ProductId)
                .AsNoTracking()
                .ProjectTo<AdminProductDetailQueryResult>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
            
            _productRules.ShouldAdminProductDetailExist(prd, request.ProductId);

            return prd!;
        }, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Admin ürün detayı bulundu. Id:{slug}", request.ProductId);

        return new GeneralSuccessResponseWithData<AdminProductDetailQueryResult>(data: product);
    }
}