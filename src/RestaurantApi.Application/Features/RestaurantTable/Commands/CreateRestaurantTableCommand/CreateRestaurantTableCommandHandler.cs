using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;

public class CreateRestaurantTableCommandHandler: IRequestHandler<CreateRestaurantTableCommand, BaseResponse>
{
    private readonly ILogger<CreateRestaurantTableCommandHandler> _logger;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cacheService;

    public CreateRestaurantTableCommandHandler(ILogger<CreateRestaurantTableCommandHandler> logger,
        IRestaurantTableRepository restaurantTableRepository, RestaurantTableRules restaurantTableRules,
        IUnitOfWork uow, ICacheService cacheService)
    {
        _logger = logger;
        _restaurantTableRepository = restaurantTableRepository;
        _restaurantTableRules = restaurantTableRules;
        _uow = uow;
        _cacheService = cacheService;
    }

    public async Task<BaseResponse> Handle(CreateRestaurantTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa oluşturma işlemi başlatıldı. TableNumber:{TableNumber}, Capacity:{Capacity}",
            request.Number, request.Capacity);

        await ValidateTableNumberAsync(request.Number, cancellationToken);

        var table = new Domain.Entities.RestaurantTable
        {
            Number = request.Number,
            Capacity = request.Capacity,
            IsActive = true
        };

        _restaurantTableRepository.CreateRestaurantTable(table, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Masa veritabanına kaydedildi. TableId:{TableId}, TableNumber:{TableNumber}, Capacity:{Capacity}, IsActive:{IsActive}",
            table.Id, table.Number, table.Capacity, table.IsActive);

        await _cacheService.RemoveAsync(CacheKeys.AdminRestaurantTables());

        _logger.LogInformation("Admin masa listesi önbelleği temizlendi. CacheKey:{CacheKey}",
            CacheKeys.AdminRestaurantTables());

        _logger.LogInformation("Masa oluşturma işlemi başarıyla tamamlandı. TableId:{TableId}, TableNumber:{TableNumber}",
            table.Id, table.Number);

        return new BaseResponse
        {
            Code = Codes.CONTENT_CREATED_SUCCESS,
            Message = "Masa başarılı bir şekilde oluşturuldu."
        };
    }

    #region Helper Methods

    private async Task ValidateTableNumberAsync(int tableNumber, CancellationToken ctx)
    {
        _logger.LogDebug("Masa numarası benzersizlik kontrolü yapılıyor. TableNumber:{TableNumber}", tableNumber);

        var isNumberTaken = await _restaurantTableRepository.GetAllAsQueryable()
            .AnyAsync(t => t.Number == tableNumber, ctx);

        _restaurantTableRules.ShouldTableNumberBeUnique(isNumberTaken, tableNumber);
    }

    #endregion
}
