using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Models.Dtos.PaymentDtos;

// İmzası doğrulanmış Stripe olayından siparişi güncellemek için ihtiyaç duyduğumuz alanlar.
// Amaç: Application katmanının sağlayıcı (Stripe SDK) tiplerine bağımlı olmaması.
public record StripeWebhookEvent
{
    // Sağlayıcının olay kimliği (loglama ve mükerrer olay takibi için).
    public string EventId { get; init; } = null!;

    public StripeWebhookEventTypes EventType { get; init; } = StripeWebhookEventTypes.Unknown;

    // Oturum açılırken metadata'ya yazılan sipariş kimliği (order_id).
    public string? OrderId { get; init; }

    // Stripe checkout oturum kimliği (sipariş kaydındaki CheckoutSessionId ile eşleşir).
    public string? CheckoutSessionId { get; init; }

    // Oturumun ödeme durumu ("paid", "unpaid", "no_payment_required").
    public string? SessionPaymentStatus { get; init; }
}
