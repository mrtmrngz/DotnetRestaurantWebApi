using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Features.Rules.UserRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Reservation.Commands.CreateCurrentUserReservationCommand;

public class CreateCurrentUserReservationCommandHandler: IRequestHandler<CreateCurrentUserReservationCommand, BaseResponse>
{
    private readonly ILogger<CreateCurrentUserReservationCommandHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationService _reservationService;
    private readonly UserRules _userRules;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;

    public CreateCurrentUserReservationCommandHandler(ILogger<CreateCurrentUserReservationCommandHandler> logger,
        IUserRepository userRepository, IReservationRepository reservationRepository,
        IReservationService reservationService, UserRules userRules, IUnitOfWork uow, TimeProvider timeProvider)
    {
        _logger = logger;
        _userRepository = userRepository;
        _reservationRepository = reservationRepository;
        _reservationService = reservationService;
        _userRules = userRules;
        _uow = uow;
        _timeProvider = timeProvider;
    }

    #region Handle

    public async Task<BaseResponse> Handle(CreateCurrentUserReservationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Rezervasyon oluşturma işlemi başlatıldı. UserId:{UserId}, TableId:{TableId}, GuestCount:{GuestCount}, StartTime:{StartTime}, EndTime:{EndTime}",
            request.UserId, request.TableId, request.GuestCount, request.StartTime, request.EndTime);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // gelen start-time/end-time YEREL saat kabul edilir; veritabanına (timestamp with time zone) UTC karşılığı yazılır
        var startTimeUtc = ReservationSchedule.ToUtcTime(request.StartTime);
        var endTimeUtc = ReservationSchedule.ToUtcTime(request.EndTime);

        // (1) kullanıcı gerçekten var mı kontrol edilir

        var user = await _userRepository.GetByIdAsync(request.UserId);

        await _userRules.UserShouldExist404(user);

        await _uow.BeginTransaction(cancellationToken);

        try
        {
            // (2) masa satırı kilitlenir (SELECT ... FOR UPDATE) ve rezervasyona uygunluğu servis üzerinden doğrulanır

            await _reservationService.ShouldTableBeReservableAsync(request.TableId, request.GuestCount, startTimeUtc,
                endTimeUtc, now, isPanelRequest: false, cancellationToken);

            // (3) rezervasyon alanları doldurulup kaydedilir, ardından transaction commit edilir

            var reservation = CreateReservationEntity(request, user!, startTimeUtc, endTimeUtc, now);

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
            "Rezervasyon oluşturma işlemi başarıyla tamamlandı. UserId:{UserId}, TableId:{TableId}",
            request.UserId, request.TableId);

        return new BaseResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "Rezervasyon başarılı bir şekilde oluşturuldu."
        };
    }

    #endregion

    #region Helper Methods

    private static Domain.Entities.Reservation CreateReservationEntity(CreateCurrentUserReservationCommand request,
        AppUser user, DateTime startTime, DateTime endTime, DateTime now)
    {
        return new Domain.Entities.Reservation
        {
            Id = Guid.NewGuid(),
            GuestCount = request.GuestCount,
            StartTime = startTime,
            EndTime = endTime,
            FullName = user.GetFullName(),
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            Note = request.Note ?? string.Empty,
            IsDeleted = false,
            RestaurantTableId = request.TableId,
            UserId = request.UserId
        };
    }

    #endregion
}
