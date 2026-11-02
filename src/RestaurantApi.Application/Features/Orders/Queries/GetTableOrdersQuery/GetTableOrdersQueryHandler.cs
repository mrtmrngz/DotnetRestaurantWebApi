using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;

public class GetTableOrdersQueryHandler
    : IRequestHandler<GetTableOrdersQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>>
{
    private readonly ILogger<GetTableOrdersQueryHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _restaurantTableRepository;
    private readonly RestaurantTableRules _restaurantTableRules;
    private readonly IMapper _mapper;

    public GetTableOrdersQueryHandler(ILogger<GetTableOrdersQueryHandler> logger,
        IOrderRepository orderRepository, IRestaurantTableRepository restaurantTableRepository,
        RestaurantTableRules restaurantTableRules, IMapper mapper)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _restaurantTableRepository = restaurantTableRepository;
        _restaurantTableRules = restaurantTableRules;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>> Handle(
        GetTableOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Masa siparişleri listeleme işlemi başlıyor. TableId:{TableId}", request.TableId);

        // (1) masa db'de var mı? (aktiflik durumu bu listede önemsizdir; pasif masanın siparişleri de listelenebilir)
        await ValidateTableExistsAsync(request.TableId, cancellationToken);

        // (2) masaya bağlı siparişler, en yeni sipariş en üstte olacak şekilde listelenir
        IReadOnlyList<GetTableOrdersQueryResult> orders = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .Where(x => x.RestaurantTableId == request.TableId)
            .OrderByDescending(x => x.OrderDate)
            .ProjectTo<GetTableOrdersQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        // Siparişi olmayan masa için hata değil boş liste döner: liste uçlarında "kayıt yok" bir hata değildir.
        if (orders.Count == 0)
        {
            _logger.LogInformation("Masanın siparişi bulunamadı. TableId:{TableId}", request.TableId);

            return new GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>(data: orders);
        }

        _logger.LogInformation("Masa siparişleri listelendi. TableId:{TableId}, OrderCount:{OrderCount}",
            request.TableId, orders.Count);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>(data: orders);
    }

    #region Helper Methods

    // Masa kaydı bulunmalıdır; aktif/pasif ayrımı yapılmaz (pasif masa da bulunur, 404 dönmez).
    private async Task ValidateTableExistsAsync(Guid tableId, CancellationToken ctx)
    {
        Domain.Entities.RestaurantTable? table = await _restaurantTableRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tableId, ctx);

        _restaurantTableRules.ShouldTableExist(table, tableId);

        _logger.LogInformation("Masa bulundu. TableId:{TableId}, IsActive:{IsActive}", tableId, table!.IsActive);
    }

    #endregion
}
