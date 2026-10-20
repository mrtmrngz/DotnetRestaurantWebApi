using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Reservation.Commands.CancelReservationCommand;

public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, BaseResponse>
{
    private readonly IReservationService _reservationService;
    private readonly ReservationRules _reservationRules;
    private readonly ILogger<CancelReservationCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelReservationCommandHandler(IReservationService reservationService, ReservationRules reservationRules,
        ILogger<CancelReservationCommandHandler> logger, TimeProvider timeProvider, IUnitOfWork unitOfWork)
    {
        _reservationService = reservationService;
        _reservationRules = reservationRules;
        _logger = logger;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rezervasyon iptal işlemi başşıyor. Request:{req}, ReservationId:{resId}",
            request.RequestType, request.ReservationId);

        // find reservation
        
        Domain.Entities.Reservation reservation = await FindReservation(request.ReservationId, request.UserId,
            request.RequestType, cancellationToken);
        
        // validate reservation status
        _reservationRules.ShouldReservationNotCanceledOrCompleted(reservation.Status, request.ReservationId, request.RequestType);
        
        // validate reservation time
        
        var utcNow = _timeProvider.GetUtcNow();
        
        _reservationRules.ShouldReservationEndTimeNotPassing(reservation.EndTime, utcNow, request.ReservationId);

        // update permission
        reservation.Status = ReservationStatus.Canceled;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Rezervasyon başarılı bir şekilde iptal edildi.");

        return new BaseResponse
        {
            Message = "Rezervasyon başarılı bir şekilde iptal edildi.",
            Code = Codes.RESERVATION_CANCELLED_SUCCESS
        };
    }

    #region Helper Methods

    private async Task<Domain.Entities.Reservation> FindReservation(Guid reservationId, Guid? userId,
        CancelReservationTypes reqType, CancellationToken ctx)
    {
        if (reqType == CancelReservationTypes.CurrentUser)
        {
            _logger.LogInformation("Kullanıcı rezervasyonu aranıyor... UserId:{userId}, ReservationId:{ReservationId}", userId, reservationId);
            _reservationRules.ShouldUserIdExistOnCurrentUserReservationCancel(userId, reservationId);

            return await _reservationService.FindAndValidateCurrentUserReservation(reservationId, userId!.Value, ctx);
        }
        
        _logger.LogInformation("Rezervasyon aranıyor..., ReservationId:{ReservationId}", reservationId);
        return await _reservationService.FindAndValidatePanelReservation(reservationId, ctx);
    }

    #endregion
}