using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.CategoryRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;

public class CategoryDetailQueryHandler : IRequestHandler<CategoryDetailQuery,
    GeneralSuccessResponseWithData<CategoryDetailQueryResult>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryRules _categoryRules;
    private readonly IMapper _mapper;
    private readonly IMediaRepository _mediaRepository;

    public CategoryDetailQueryHandler(ICategoryRepository categoryRepository, CategoryRules categoryRules,
        IMapper mapper, IMediaRepository mediaRepository)
    {
        _categoryRepository = categoryRepository;
        _categoryRules = categoryRules;
        _mapper = mapper;
        _mediaRepository = mediaRepository;
    }

    public async Task<GeneralSuccessResponseWithData<CategoryDetailQueryResult>> Handle(CategoryDetailQuery request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetAllAsQueryable()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == request.CategoryId)
            .ProjectTo<CategoryDetailQueryResult>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        await _categoryRules.ShouldCategoryDetailExist(category);

        return new GeneralSuccessResponseWithData<CategoryDetailQueryResult>(data:category!);
    }
}