using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Reservation.Commands.CreatePanelReservationCommand;

public class CreatePanelReservationCommandHandler : IRequestHandler<CreatePanelReservationCommand, BaseResponse>
{
    private readonly ILogger<CreatePanelReservationCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationService _reservationService;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;

    public CreatePanelReservationCommandHandler(ILogger<CreatePanelReservationCommandHandler> logger,
        IUserRepository userRepository, IReservationRepository reservationRepository,
        IReservationService reservationService, IUnitOfWork uow, TimeProvider timeProvider)
    {
        _logger = logger;
        _userRepository = userRepository;
        _reservationRepository = reservationRepository;
        _reservationService = reservationService;
        _uow = uow;
        _timeProvider = timeProvider;
    }

    public async Task<BaseResponse> Handle(CreatePanelReservationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Yetkili Kullanıcılar için rezervasyon oluşturma işlemi başlatıldı. TableId:{TableId}, GuestCount:{GuestCount}, StartTime:{StartTime}, EndTime:{EndTime}",
            request.TableId, request.GuestCount, request.StartTime, request.EndTime);
        
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        
        var startTimeUtc = ReservationSchedule.ToUtcTime(request.StartTime);
        var endTimeUtc = ReservationSchedule.ToUtcTime(request.EndTime);

        await _uow.BeginTransaction(cancellationToken);

        try
        {
            await _reservationService.ShouldTableBeReservableAsync(request.TableId, request.GuestCount, startTimeUtc,
                endTimeUtc, now, isPanelRequest: true, cancellationToken);

            Domain.Entities.Reservation reservation =
                CreateReservationEntity(request, startTimeUtc, endTimeUtc, now);
            
            _reservationRepository.CreateReservation(reservation, cancellationToken);
            
            await _uow.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Rezervasyon veritabanına kaydedildi. ReservationId:{ReservationId}, TableId:{TableId}, Status:{Status}",
                reservation.Id, reservation.RestaurantTableId, reservation.Status);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
        
        _logger.LogInformation(
            "Yetkili Kullanıcılar için rezervasyon oluşturma işlemi başarıyla tamamlandı. UserId:{UserId}, TableId:{TableId}",
            request.TableId);

        return new BaseResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "Rezervasyon başarılı bir şekilde oluşturuldu."
        };
    }

    #region Helpers

    private static Domain.Entities.Reservation CreateReservationEntity(CreatePanelReservationCommand request,
        DateTime startTime, DateTime endTime, DateTime now)
    {
        return new Domain.Entities.Reservation
        {
            Id = Guid.NewGuid(),
            GuestCount = request.GuestCount,
            StartTime = startTime,
            EndTime = endTime,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Status = ReservationStatus.Confirmed,
            CreatedAt = now,
            Note = request.Note ?? string.Empty,
            IsDeleted = false,
            RestaurantTableId = request.TableId,
        };
    }

    #endregion
}