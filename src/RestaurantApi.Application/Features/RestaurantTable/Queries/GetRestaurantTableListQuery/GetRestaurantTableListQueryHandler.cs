using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;

public class GetRestaurantTableListQueryHandler: IRequestHandler<GetRestaurantTableListQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetRestaurantTableListQueryHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IMapper _mapper;

    public GetRestaurantTableListQueryHandler(ICacheService cacheService,
        ILogger<GetRestaurantTableListQueryHandler> logger, IRestaurantTableRepository restaurantTableRepository,
        IMapper mapper)
    {
        _cacheService = cacheService;
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _mapper = mapper;
    }

    #region Handle

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>> Handle(
        GetRestaurantTableListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Masa listesi getiriliyor... IsActive:{IsActive}, MinGuest:{MinGuest}, MaxGuest:{MaxGuest}, Number:{Number}",
            request.IsActive, request.MinGuest, request.MaxGuest, request.Number);

        var tables = await _cacheService.GetOrInternalSetAsync(CacheKeys.AdminRestaurantTables(),
            () => BuildTableListAsync(cancellationToken), TimeSpan.FromMinutes(30));

        IReadOnlyList<GetRestaurantTableListQueryResult> result = FilterTables(tables, request);

        _logger.LogInformation("Masa listesi bulundu. Count:{Count}", result.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>(data: result);
    }

    #endregion

    #region Helper Methods

    private async Task<IReadOnlyList<GetRestaurantTableListQueryResult>> BuildTableListAsync(CancellationToken ctx)
    {
        return await _restaurantTableRepository.GetAllAsQueryable()
            .AsNoTracking()
            .OrderBy(t => t.Number)
            .ProjectTo<GetRestaurantTableListQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(ctx);
    }

    private static IReadOnlyList<GetRestaurantTableListQueryResult> FilterTables(
        IReadOnlyList<GetRestaurantTableListQueryResult> tables, GetRestaurantTableListQuery request)
    {
        return tables
            .Where(t => !request.IsActive.HasValue || t.IsActive == request.IsActive.Value)
            .Where(t => !request.MinGuest.HasValue || t.GuestCount >= request.MinGuest.Value)
            .Where(t => !request.MaxGuest.HasValue || t.GuestCount <= request.MaxGuest.Value)
            .Where(t => string.IsNullOrWhiteSpace(request.Number) ||
                        t.Number.ToString().Contains(request.Number.Trim()))
            .ToList();
    }

    #endregion
}
