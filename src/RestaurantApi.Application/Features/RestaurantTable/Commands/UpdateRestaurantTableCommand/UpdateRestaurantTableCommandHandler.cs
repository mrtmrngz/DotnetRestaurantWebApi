using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.UpdateRestaurantTableCommand;

public class UpdateRestaurantTableCommandHandler: IRequestHandler<UpdateRestaurantTableCommand, BaseResponse>
{
    private readonly ILogger<UpdateRestaurantTableCommandHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;

    public UpdateRestaurantTableCommandHandler(ILogger<UpdateRestaurantTableCommandHandler> logger,
        IRestaurantTableRepository restaurantTableRepository, IReservationRepository reservationRepository,
        RestaurantTableRules restaurantTableRules, IUnitOfWork uow, ICacheService cacheService)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _reservationRepository = reservationRepository;
        _restaurantTableRules = restaurantTableRules;
        _uow = uow;
        _cacheService = cacheService;
    }

    #region Handle

    public async Task<BaseResponse> Handle(UpdateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa güncelleme işlemi başlıyor... TableId:{TableId}", request.TableId);

        await _uow.BeginTransaction(cancellationToken);

        try
        {
            // (1) masa satırı kilitlenir (SELECT ... FOR UPDATE)

            var table = await _restaurantTableRepository.GetByIdForUpdateAsync(request.TableId, cancellationToken);

            _restaurantTableRules.ShouldTableExist(table, request.TableId);

            // (2) rezervasyon kontrolleri (tamamı DB tarafında çalışır)
            // bitiş zamanı geçmiş rezervasyonlar aktif kabul edilmez

            var now = DateTime.UtcNow;

            var hasActiveReservation =
                await _reservationRepository.HasActiveReservationAsync(request.TableId, now, cancellationToken);

            var maxActiveGuestCount = hasActiveReservation
                ? await _reservationRepository.GetMaxActiveGuestCountAsync(request.TableId, now, cancellationToken)
                : 0;

            _restaurantTableRules.ShouldTableBeUpdatableWithReservations(hasActiveReservation, maxActiveGuestCount,
                request.Capacity, request.IsActive);

            if (request.Number.HasValue && request.Number.Value != table!.Number)
            {
                var isNumberTaken = await _restaurantTableRepository.IsTableNumberTakenAsync(request.Number.Value,
                    request.TableId, cancellationToken);

                _restaurantTableRules.ShouldTableNumberBeUnique(isNumberTaken, request.Number.Value);
            }

            // (3) güncelleme yapılır ve transaction commit edilir

            UpdateTableFields(table!, request);

            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Masa başarılı bir şekilde güncellendi. TableId:{TableId}", request.TableId);

        await _cacheService.RemoveAsync(CacheKeys.AdminRestaurantTables());

        _logger.LogInformation("Admin masa listesi önbelleği temizlendi. CacheKey:{CacheKey}",
            CacheKeys.AdminRestaurantTables());

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "Masa başarılı bir şekilde güncellendi."
        };
    }

    #endregion

    #region Helper Methods

    private void UpdateTableFields(Domain.Entities.RestaurantTable table, UpdateRestaurantTableCommand request)
    {
        _logger.LogInformation(
            "Masa alanları güncelleniyor. TableId:{TableId}, OldNumber:{OldNumber}, NewNumber:{NewNumber}, OldCapacity:{OldCapacity}, NewCapacity:{NewCapacity}, OldIsActive:{OldIsActive}, NewIsActive:{NewIsActive}",
            table.Id, table.Number, request.Number, table.Capacity, request.Capacity, table.IsActive, request.IsActive);

        if (request.Number.HasValue)
            table.Number = request.Number.Value;

        if (request.Capacity.HasValue)
            table.Capacity = request.Capacity.Value;

        if (request.IsActive.HasValue)
            table.IsActive = request.IsActive.Value;

        _logger.LogInformation(
            "Masa alanları güncellendi. TableId:{TableId}, Number:{Number}, Capacity:{Capacity}, IsActive:{IsActive}",
            table.Id, table.Number, table.Capacity, table.IsActive);
    }

    #endregion
}
