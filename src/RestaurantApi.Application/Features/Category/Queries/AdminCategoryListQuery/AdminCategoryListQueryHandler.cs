using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;

public class AdminCategoryListQueryHandler: IRequestHandler<AdminCategoryListQuery, GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>>
{
    private readonly ILogger<AdminCategoryListQueryHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public AdminCategoryListQueryHandler(ILogger<AdminCategoryListQueryHandler> logger, ICategoryRepository categoryRepository, ICacheService cacheService, IMapper mapper)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>> Handle(AdminCategoryListQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.AdminCategories();
        IReadOnlyList<AdminCategoryListQueryResult> categories = await _cacheService.GetOrInternalSetAsync(cacheKey,
            async () =>
            {
                return await _categoryRepository
                    .GetAllAsQueryable()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .OrderBy(c => c.IsDeleted)
                    .ThenBy(c => c.Title)
                    .ProjectTo<AdminCategoryListQueryResult>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);
            }, TimeSpan.FromHours(1));

        return new GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>(data: categories);
    }
}