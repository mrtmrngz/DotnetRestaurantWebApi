using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Features.Rules.OrderRules;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Infrastructure.Services;

// Siparişe ait ortak veri erişimi. Kurallar OrderRules içinde tanımlıdır; bu servis onları çağırarak
// "siparişi bul + yoksa hata ver" adımını akışların tekrar yazmasını engeller.
public class OrderService: IOrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderRules _orderRules;

    public OrderService(ILogger<OrderService> logger, IOrderRepository orderRepository, OrderRules orderRules)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _orderRules = orderRules;
    }

    public async Task<Order> FindAndValidateOrderAsync(Guid orderId, CancellationToken ctx)
    {
        _logger.LogInformation("Sipariş getiriliyor. OrderId:{OrderId}", orderId);

        Order? order = await _orderRepository.GetAllAsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId, ctx);

        // sipariş yoksa 404
        _orderRules.ShouldOrderExist(order, orderId);

        _logger.LogInformation("Sipariş bulundu. OrderId:{OrderId}", orderId);

        return order!;
    }
}
