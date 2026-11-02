using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;

public class GetAllOrdersQueryHandler
    : IRequestHandler<GetAllOrdersQuery, GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>>
{
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetAllOrdersQueryHandler(ILogger<GetAllOrdersQueryHandler> logger, IOrderRepository orderRepository,
        IMapper mapper)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>> Handle(
        GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Panel sipariş listeleme işlemi başlıyor. Type:{Type}, OrderStatus:{OrderStatus}, PaymentStatus:{PaymentStatus}",
            request.Type, request.OrderStatus, request.PaymentStatus);

        // (1) filtreler çözümlenir; geçersiz bir filtre değeri hata değil boş liste ile karşılanır
        if (!TryResolveFilters(request, out OrderListFilters filters))
        {
            _logger.LogWarning(
                "Geçersiz sipariş listeleme filtresi; boş liste dönülüyor. Type:{Type}, OrderStatus:{OrderStatus}, PaymentStatus:{PaymentStatus}",
                request.Type, request.OrderStatus, request.PaymentStatus);

            return new GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>(
                data: Array.Empty<GetAllOrdersQueryResult>());
        }

        IQueryable<Domain.Entities.Order> ordersQuery = _orderRepository.GetAllAsQueryable()
            .AsNoTracking();

        if (filters.Type is not null)
        {
            ordersQuery = filters.Type == GetAllOrdersQuery.TypeTable
                ? ordersQuery.Where(x => x.RestaurantTableId != null)
                : ordersQuery.Where(x => x.UserId != null);
        }

        if (filters.OrderStatus.HasValue)
        {
            ordersQuery = ordersQuery.Where(x => x.OrderStatus == filters.OrderStatus.Value);
        }

        if (filters.PaymentStatus.HasValue)
        {
            ordersQuery = ordersQuery.Where(x => x.PaymentStatus == filters.PaymentStatus.Value);
        }

        IReadOnlyList<GetAllOrdersQueryResult> orders = await ordersQuery
            .OrderByDescending(x => x.OrderDate)
            .ProjectTo<GetAllOrdersQueryResult>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Panel sipariş listesi hazırlandı. OrderCount:{OrderCount}, Type:{Type}, OrderStatus:{OrderStatus}, PaymentStatus:{PaymentStatus}",
            orders.Count, filters.Type, filters.OrderStatus, filters.PaymentStatus);

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>(data: orders);
    }

    #region Helper Methods

    // Filtreleri çözümler: tip yalnızca "table"/"users"; durumlar OrderStatus/PaymentStatus enum'larıyla eşleşmelidir.
    // Değer boşsa filtre uygulanmaz (tüm siparişler döner); değer geçersizse false döner ve çağıran boş liste üretir.
    private static bool TryResolveFilters(GetAllOrdersQuery request, out OrderListFilters filters)
    {
        filters = default;

        string? type = null;

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            type = request.Type.Trim().ToLowerInvariant();

            bool typeIsSupported = type is GetAllOrdersQuery.TypeTable or GetAllOrdersQuery.TypeUsers;

            if (!typeIsSupported)
            {
                return false;
            }
        }

        OrderStatus? orderStatus = null;

        if (!string.IsNullOrWhiteSpace(request.OrderStatus))
        {
            if (!Enum.TryParse(request.OrderStatus, ignoreCase: true, out OrderStatus parsedOrderStatus)
                || !Enum.IsDefined(parsedOrderStatus))
            {
                return false;
            }

            orderStatus = parsedOrderStatus;
        }

        PaymentStatus? paymentStatus = null;

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            if (!Enum.TryParse(request.PaymentStatus, ignoreCase: true, out PaymentStatus parsedPaymentStatus)
                || !Enum.IsDefined(parsedPaymentStatus))
            {
                return false;
            }

            paymentStatus = parsedPaymentStatus;
        }

        filters = new OrderListFilters(type, orderStatus, paymentStatus);

        return true;
    }

    #endregion

    private readonly record struct OrderListFilters(string? Type, OrderStatus? OrderStatus,
        PaymentStatus? PaymentStatus);
}
