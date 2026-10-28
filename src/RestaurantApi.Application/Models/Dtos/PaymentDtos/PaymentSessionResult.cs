namespace RestaurantApi.Application.Models.Dtos.PaymentDtos;

// Ödeme sağlayıcısından (Stripe) dönen oturum bilgileri.
public record PaymentSessionResult(string SessionId, string SessionUrl, DateTime ExpiresAt);
