using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;

public class GetAvailableTablesQueryHandler: IRequestHandler<GetAvailableTablesQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>
{
    private readonly ILogger<GetAvailableTablesQueryHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IMapper _mapper;

    public GetAvailableTablesQueryHandler(ILogger<GetAvailableTablesQueryHandler> logger,
        IRestaurantTableRepository restaurantTableRepository, IReservationRepository reservationRepository,
        IMapper mapper)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>> Handle(
        GetAvailableTablesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Müsait masalar getiriliyor... GuestCount:{GuestCount}, StartTime:{StartTime}, EndTime:{EndTime}",
            request.GuestCount, request.StartTime, request.EndTime);

        var now = DateTime.UtcNow;

        // bitiş zamanı geçmiş rezervasyonlar meşgul sayılmaz

        var busyTableIds = await _reservationRepository.GetBusyTableIdsAsync(now, cancellationToken);

        var availableTables = await _restaurantTableRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive && t.Capacity >= request.GuestCount && !busyTableIds.Contains(t.Id))
            .OrderBy(t => t.Capacity)
            .ProjectTo<GetAvailableTablesQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Müsait masalar bulundu. Count:{Count}, GuestCount:{GuestCount}",
            availableTables.Count, request.GuestCount);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>(data: availableTables);
    }
}
