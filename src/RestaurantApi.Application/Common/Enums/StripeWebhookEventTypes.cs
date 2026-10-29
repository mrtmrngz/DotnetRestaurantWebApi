namespace RestaurantApi.Application.Common.Enums;

// Stripe checkout oturumu webhook olaylarından işlediklerimiz.
// Sağlayıcıya özel event isimleri Infrastructure katmanında bu enum'a çevrilir;
// böylece Application katmanı Stripe tiplerine bağımlı kalmaz.
public enum StripeWebhookEventTypes
{
    // İlgi alanımıza girmeyen ya da henüz desteklenmeyen olay.
    Unknown = 0,
    CheckoutSessionCompleted,
    CheckoutSessionAsyncPaymentSucceeded,
    CheckoutSessionAsyncPaymentFailed,
    CheckoutSessionExpired
}
