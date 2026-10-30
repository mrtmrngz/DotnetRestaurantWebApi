using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Rules.OrderRules;

// Kullanıcı siparişi durum güncelleme akışının iş kuralları.
// Her kural, ilgili durumda log yazıp uygun iş kuralı hatasını fırlatır.
public class OrderRules
{
    private readonly ILogger<OrderRules> _logger;

    public OrderRules(ILogger<OrderRules> logger)
    {
        _logger = logger;
    }

    // (404) Güncellenmek istenen sipariş yoksa.
    public void ShouldOrderExist(Domain.Entities.Order? order, Guid orderId)
    {
        if (order is not null)
            return;

        _logger.LogWarning("Güncellenmek istenen sipariş bulunamadı. OrderId:{OrderId}", orderId);
        throw new NotFoundException("Sipariş bulunamadı.");
    }

    // (403) Sipariş, isteği yapan kullanıcıya ait değilse güncellenemez.
    // Masa siparişlerinde UserId boş olduğu için bu kural onları da kapsar: masa siparişi bu uçtan güncellenmez.
    public void ShouldOrderBelongToUser(Domain.Entities.Order order, Guid userId)
    {
        if (order.UserId.HasValue && order.UserId.Value == userId)
            return;

        _logger.LogWarning(
            "Sipariş, isteği yapan kullanıcıya ait değil. OrderId:{OrderId}, OrderUserId:{OrderUserId}, RequestUserId:{RequestUserId}",
            order.Id, order.UserId, userId);

        throw new ForbiddenException("Bu sipariş size ait değil.");
    }

    // (409) Sipariş zaten istenen durumdaysa güncelleme yapılmaz.
    public void ShouldOrderStatusBeDifferent(OrderStatus currentStatus, OrderStatus requestedStatus, Guid orderId)
    {
        if (currentStatus != requestedStatus)
            return;

        _logger.LogWarning("Sipariş zaten istenen durumda. OrderId:{OrderId}, OrderStatus:{OrderStatus}",
            orderId, currentStatus);

        throw new ConflictException($"Sipariş zaten {currentStatus} durumunda.");
    }

    // (422) Ödemesi tamamlanmayan siparişin durumu güncellenemez (ödeme alınmadan üretime geçilmemelidir).
    public void ShouldOrderBePaid(PaymentStatus paymentStatus, Guid orderId)
    {
        if (paymentStatus == PaymentStatus.Paid)
            return;

        _logger.LogWarning(
            "Ödemesi tamamlanmayan siparişin durumu güncellenemez. OrderId:{OrderId}, PaymentStatus:{PaymentStatus}",
            orderId, paymentStatus);

        throw new UnprocessableEntityError("Ödemesi tamamlanmayan siparişin durumu güncellenemez.");
    }

    // (409) Eşzamanlı bir değişiklik nedeniyle güncelleme uygulanamadıysa (ör. ödeme durumu tam bu anda değişti).
    public void ShouldOrderUpdateBeApplied(int affectedRowCount, Guid orderId)
    {
        if (affectedRowCount > 0)
            return;

        _logger.LogWarning(
            "Sipariş durumu güncellenemedi; sipariş bu sırada değişmiş olabilir. OrderId:{OrderId}", orderId);

        throw new ConflictException("Sipariş durumu güncellenemedi. Lütfen tekrar deneyiniz.");
    }

    public void ShouldOrderBelongToCurrentUser(Domain.Entities.Order order, Guid userId)
    {
        if (order.UserId.HasValue && order.UserId.Value == userId)
            return;

        _logger.LogWarning(
            "Sipariş, isteği yapan kullanıcıya ait değil. OrderId:{OrderId}, OrderUserId:{OrderUserId}, CurrentUserId:{CurrentUserId}",
            order.Id, order.UserId, userId);

        throw new NotFoundException("Sipariş bulunamadı.");
    }

    // İptal edilemeyecek durumdaki siparişler: iptal edilmişse 409, teslim edilmişse 422.
    public void ShouldOrderBeCancelable(OrderStatus orderStatus, Guid orderId)
    {
        if (orderStatus == OrderStatus.Canceled)
        {
            _logger.LogWarning("Sipariş zaten iptal edilmiş. OrderId:{OrderId}", orderId);
            throw new ConflictException("Sipariş zaten iptal edilmiş.");
        }

        if (orderStatus == OrderStatus.Delivered)
        {
            _logger.LogWarning("Teslim edilmiş sipariş iptal edilemez. OrderId:{OrderId}", orderId);
            throw new UnprocessableEntityError("Sipariş teslim edildiği için iptal edilemez.");
        }
    }

}
