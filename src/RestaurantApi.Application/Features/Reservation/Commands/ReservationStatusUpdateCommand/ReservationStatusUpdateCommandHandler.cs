using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;

public class ReservationStatusUpdateCommandHandler : IRequestHandler<ReservationStatusUpdateCommand, BaseResponse>
{
    private readonly ILogger<ReservationStatusUpdateCommandHandler> _logger;
    private readonly IReservationRepository _reservationRepository;
    private readonly ReservationRules _reservationRules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ReservationStatusUpdateCommandHandler(ILogger<ReservationStatusUpdateCommandHandler> logger,
        IReservationRepository reservationRepository, ReservationRules reservationRules, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _logger = logger;
        _reservationRepository = reservationRepository;
        _reservationRules = reservationRules;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<BaseResponse> Handle(ReservationStatusUpdateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rezervasyon durum güncelleme işlemi başlıyor...");

        // search reservation
        var reservation = await _reservationRepository.GetAllAsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.ReservationId, cancellationToken);

        // validate reservation exist
        _reservationRules.ShouldReservationExist(reservation, request.ReservationId);

        var nowUtc = _timeProvider.GetUtcNow();
        
        // validate reservation end time not passing
        _reservationRules.ShouldReservationEndTimeNotPassing(reservation!.EndTime, nowUtc, request.ReservationId);

        // update reservation if request different of current status
        if (reservation!.Status != request.Status)
        {
            reservation.Status = request.Status;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Rezervasyon durum güncelleme işlemi başarılı bir şekilde tamamlandı. ReservationID:{ResId} YeniDurum:{Status}",
            request.ReservationId, request.Status);

        // response return
        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = $"Rezarvasyon durumu {request.Status} olarak başarılı bir şekilde güncellendi."
        };
    }
}