namespace RestaurantApi.Application.Common.Abstractions.Services;

// Siparişe ait ortak işlemler. "Siparişi getir ve yokluğunu doğrula" adımı tek noktada toplanır;
// böylece siparişe ihtiyaç duyan tüm akışlar (durum güncelleme, iptal, detay vb.) aynı kontrolü
// tekrar yazmak zorunda kalmaz.
public interface IOrderService
{
    // Siparişi id'ye göre getirir; sipariş yoksa iş kuralı hatası fırlatır (404).
    // Yalnızca okuyan akışlar için çalıştığından AsNoTracking ile getirir.
    Task<Domain.Entities.Order> FindAndValidateOrderAsync(Guid orderId, CancellationToken ctx);
}
