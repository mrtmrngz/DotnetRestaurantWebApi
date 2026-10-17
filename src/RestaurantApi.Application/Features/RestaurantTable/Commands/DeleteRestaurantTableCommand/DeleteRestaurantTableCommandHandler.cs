using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.DeleteRestaurantTableCommand;

public class DeleteRestaurantTableCommandHandler: IRequestHandler<DeleteRestaurantTableCommand, BaseResponse>
{
    private readonly ILogger<DeleteRestaurantTableCommandHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;

    public DeleteRestaurantTableCommandHandler(ILogger<DeleteRestaurantTableCommandHandler> logger,
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

    public async Task<BaseResponse> Handle(DeleteRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa silme işlemi başlıyor... TableId:{TableId}", request.TableId);

        await _uow.BeginTransaction(cancellationToken);

        int affectedRows;

        try
        {
            // (1) masa satırı kilitlenir (SELECT ... FOR UPDATE)

            var table = await _restaurantTableRepository.GetByIdForUpdateAsync(request.TableId, cancellationToken);

            _restaurantTableRules.ShouldTableBeValidForDeletion(table);

            // (2) aktif rezervasyon kontrolü (bitiş zamanı geçmiş rezervasyonlar hariç)

            var now = DateTime.UtcNow;

            var hasActiveReservation =
                await _reservationRepository.HasActiveReservationAsync(request.TableId, now, cancellationToken);

            _restaurantTableRules.ShouldTableNotHaveActiveReservationForDeletion(hasActiveReservation,
                request.TableId);

            // (3) soft delete (pasife alma); change tracker atlanır ve tek sorguda güncellenir

            affectedRows = await _restaurantTableRepository.GetAllAsQueryable()
                .Where(t => t.Id == request.TableId)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.IsActive, false), cancellationToken);

            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation(
            "Masa başarılı bir şekilde pasife alındı (soft delete). TableId:{TableId}, EtkilenenSatır:{AffectedRows}",
            request.TableId, affectedRows);

        await _cacheService.RemoveAsync(CacheKeys.AdminRestaurantTables());

        _logger.LogInformation("Admin masa listesi önbelleği temizlendi. CacheKey:{CacheKey}",
            CacheKeys.AdminRestaurantTables());

        _logger.LogInformation("Masa silme işlemi başarıyla tamamlandı. TableId:{TableId}", request.TableId);

        return new BaseResponse
        {
            Code = Codes.CONTENT_DELETED_SUCCESS,
            Message = "Masa başarılı bir şekilde silindi."
        };
    }
}
