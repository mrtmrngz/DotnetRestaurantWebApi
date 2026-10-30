using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.OrderRules;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;

public class OrderCancelCommandHandler: IRequestHandler<OrderCancelCommand, BaseResponse>
{
    private readonly ILogger<OrderCancelCommandHandler> _logger;
    private readonly IOrderService _orderService;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderRules _orderRules;

    public OrderCancelCommandHandler(ILogger<OrderCancelCommandHandler> logger, IOrderService orderService,
        IOrderRepository orderRepository, OrderRules orderRules)
    {
        _logger = logger;
        _orderService = orderService;
        _orderRepository = orderRepository;
        _orderRules = orderRules;
    }

    public async Task<BaseResponse> Handle(OrderCancelCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sipariş iptal işlemi başlıyor. OrderId:{OrderId}, UserId:{UserId}",
            request.OrderId, request.UserId);

        // (1) Validate Order
        await ValidateOrderAsync(request.OrderId, request.UserId, cancellationToken);

        // (2) tek SQL ifadesiyle sipariş iptal edilir ve ödeme durumu iade (Refunded) olarak işaretlenir;
        //     koşullar, bu sırada değişen bir siparişin üzerine yazılmasını engeller
        int affectedRowCount = await _orderRepository.GetAllAsQueryable()
            .Where(x => x.Id == request.OrderId
                        && x.PaymentStatus == PaymentStatus.Paid
                        && x.OrderStatus != OrderStatus.Canceled
                        && x.OrderStatus != OrderStatus.Delivered)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.OrderStatus, OrderStatus.Canceled)
                .SetProperty(x => x.PaymentStatus, PaymentStatus.Refunded), cancellationToken);

        // (3) güncellemenin uygulandığı garanti altına alınır; aksi hâlde 409
        _orderRules.ShouldOrderUpdateBeApplied(affectedRowCount, request.OrderId);

        _logger.LogInformation(
            "Sipariş iptal edildi ve ödeme durumu iade olarak işaretlendi. OrderId:{OrderId}, OrderStatus:{OrderStatus}, PaymentStatus:{PaymentStatus}, AffectedRowCount:{AffectedRowCount}",
            request.OrderId, OrderStatus.Canceled, PaymentStatus.Refunded, affectedRowCount);

        return new BaseResponse
        {
            Code = Codes.CONTENT_UPDATED_SUCCESS,
            Message = "Sipariş iptal edildi ve ödeme durumu iade olarak işaretlendi."
        };
    }

    #region Helper Method

    private async Task ValidateOrderAsync(Guid orderId, Guid userId, CancellationToken ctx)
    {
        // (1) sipariş bulunur; yoksa 404 (ortak adım: IOrderService.FindAndValidateOrderAsync)
        Domain.Entities.Order order = await _orderService.FindAndValidateOrderAsync(orderId, ctx);

        // (2) sipariş isteği yapan kullanıcıya ait değilse 404 döner (başka kullanıcıya ait siparişin
        //     varlığı sızdırılmaz); gerçek sebep OrderRules içinde loglanır.
        _orderRules.ShouldOrderBelongToCurrentUser(order, userId);

        // (3) ödemesi tamamlanmayan sipariş iptal edilemez -> 422
        _orderRules.ShouldOrderBePaid(order.PaymentStatus, orderId);

        // (4) iptal edilmiş sipariş (409) ya da teslim edilmiş sipariş (422) iptal edilemez
        _orderRules.ShouldOrderBeCancelable(order.OrderStatus, orderId);
    }

    #endregion
}
