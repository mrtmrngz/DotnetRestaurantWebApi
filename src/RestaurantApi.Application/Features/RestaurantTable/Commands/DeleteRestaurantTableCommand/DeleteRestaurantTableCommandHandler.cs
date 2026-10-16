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
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly ICacheService _cacheService;

    public DeleteRestaurantTableCommandHandler(ILogger<DeleteRestaurantTableCommandHandler> logger,
        IRestaurantTableRepository restaurantTableRepository, RestaurantTableRules restaurantTableRules,
        ICacheService cacheService)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _restaurantTableRules = restaurantTableRules;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(DeleteRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa silme işlemi başlıyor... TableId:{TableId}", request.TableId);

        await ValidateTableForDeletionAsync(request.TableId, cancellationToken);

        var affectedRows = await _restaurantTableRepository.GetAllAsQueryable()
            .Where(t => t.Id == request.TableId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(t => t.IsActive, false), cancellationToken);

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

    #region Helper Methods

    private async Task ValidateTableForDeletionAsync(Guid tableId, CancellationToken ctx)
    {
        var table = await _restaurantTableRepository.GetAllAsQueryable()
            .FirstOrDefaultAsync(t => t.Id == tableId, ctx);

        _restaurantTableRules.ShouldTableBeValidForDeletion(table);
    }

    #endregion
}
