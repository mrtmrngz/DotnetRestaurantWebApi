using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

public class ReservationListQueryHandler: IRequestHandler<ReservationListQuery, GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>>
{
    private readonly ILogger<ReservationListQueryHandler> _logger;
    private readonly IReservationRepository _reservationRepository;
    private readonly IMapper _mapper;
    private readonly ReservationRules _reservationRules;

    public ReservationListQueryHandler(ILogger<ReservationListQueryHandler> logger, IReservationRepository reservationRepository, IMapper mapper, ReservationRules reservationRules)
    {
        _logger = logger;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _reservationRules = reservationRules;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>> Handle(ReservationListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rezarvasyon listeleme işlemi başlıyor... RequestType:{Type}", request.RequestType);

        IReadOnlyList<ReservationListQueryResult> 
            reservations = await FindRawReservations(request.RequestType.ToString(), request.UserId, cancellationToken);
        
        _logger.LogInformation("Rezervasyonlar başarılı bir şekilde bulundu.");

        return new GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>(data: reservations);
    }

    #region Helper Methods

    private async Task<IReadOnlyList<ReservationListQueryResult>> FindRawReservations(string requestType, Guid? userId, CancellationToken ctx)
    {

        IQueryable<Domain.Entities.Reservation> reservationsQuery = _reservationRepository.GetAllAsQueryable()
            .AsNoTracking();
        
        if (string.Equals(requestType, nameof(ReservationListRequestEnums.CurrentUser),
                StringComparison.OrdinalIgnoreCase))
        {
            _reservationRules.UserIdShouldInListRequest(userId);
            reservationsQuery = reservationsQuery.Where(x => x.UserId == userId);
        }

        return await reservationsQuery
            .OrderByDescending(x => x.CreatedAt)
            .ProjectTo<ReservationListQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(ctx);
    }

    #endregion
}