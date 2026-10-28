using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IPaymentService
{
    // Sipariş için ödeme oturumu oluşturur ve oturum bilgilerini (id, url, bitiş zamanı) döner.
    // Tutarlar sağlayıcının beklediği en küçük birime (kuruş) çevrilir, IdempotencyKey sağlayıcıya iletilir.
    Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, CheckoutRequestTypes requestType,
        CancellationToken ctx);
}
