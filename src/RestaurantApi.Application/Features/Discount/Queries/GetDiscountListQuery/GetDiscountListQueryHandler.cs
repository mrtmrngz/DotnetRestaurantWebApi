using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;

public class GetDiscountListQueryHandler: IRequestHandler<GetDiscountListQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetDiscountListQueryHandler> _logger;
    private readonly IDiscountRepository _discountRepository;
    private readonly IMapper _mapper;

    public GetDiscountListQueryHandler(ICacheService cacheService, ILogger<GetDiscountListQueryHandler> logger,
        IDiscountRepository discountRepository, IMapper mapper)
    {
        _cacheService = cacheService;
        _logger = logger;
        _discountRepository = discountRepository;
        _mapper = mapper;
    }

    #region Handle

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>> Handle(
        GetDiscountListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim listesi getiriliyor... Status:{Status}", request.Status);

        var discounts = await _cacheService.GetOrInternalSetAsync(CacheKeys.AdminDiscounts(),
            () => BuildDiscountListAsync(cancellationToken), TimeSpan.FromHours(1));

        IReadOnlyList<GetDiscountListQueryResult> result = request.Status is null
            ? discounts
            : discounts.Where(d => d.Status == request.Status).ToList();

        _logger.LogInformation("İndirim listesi bulundu. Count:{Count}", result.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>(data: result);
    }

    #endregion

    #region Helper Methods

    private async Task<IReadOnlyList<GetDiscountListQueryResult>> BuildDiscountListAsync(CancellationToken ctx)
    {
        var now = DateTime.UtcNow;

        var discounts = await _discountRepository.GetAllAsQueryable()
            .AsNoTracking()
            .ProjectTo<GetDiscountListQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(ctx);

        foreach (var discount in discounts)
        {
            discount.Status = ResolveStatus(discount.StartDate, discount.EndDate, now);
        }

        return discounts;
    }

    private static DiscountStatus ResolveStatus(DateTime startDate, DateTime endDate, DateTime now)
    {
        if (now < startDate)
            return DiscountStatus.Upcoming;

        if (now > endDate)
            return DiscountStatus.Passive;

        return DiscountStatus.Active;
    }

    #endregion
}
