using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;

public class GetCategoriesQueryHandler: IRequestHandler<GetCategoryQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>>
{
    private readonly ILogger<GetCategoriesQueryHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandler(ILogger<GetCategoriesQueryHandler> logger, ICategoryRepository categoryRepository, ICacheService cacheService, IMapper mapper)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Categories();
        IReadOnlyList<GetCategoriesQueryResult> categories = await _cacheService.GetOrInternalSetAsync(cacheKey,
            async () =>
            {
                return await _categoryRepository
                    .GetAllAsQueryable()
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.Title)
                    .ProjectTo<GetCategoriesQueryResult>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);
            }, TimeSpan.FromHours(1));

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>(data: categories);
    }
}