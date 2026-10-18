namespace RestaurantApi.Application.Common.Abstractions.Services;

public interface IReservationService
{
    // Çağrı öncesi transaction açılmış olmalıdır: masa satırı kilitlenir (SELECT ... FOR UPDATE) ve
    // istenen zaman aralığı için masanın rezervasyona uygunluğu doğrulanır:
    // masa var olmalı, aktif olmalı, kapasitesi yeterli olmalı ve aralıkla çakışan aktif rezervasyonu olmamalı.
    // Zamanlar UTC'dir (startTimeUtc/endTimeUtc yerel duvar saatinin UTC karşılığı, nowUtc ise şu an).
    // isPanelRequest: panel (admin) akışı mı? current user akışında pasif masa "masa bulunamadı" (404) olarak döner,
    // masanın aktif/pasif durumu istemciye sızdırılmaz.
    Task ShouldTableBeReservableAsync(Guid tableId, int guestCount, DateTime startTimeUtc, DateTime endTimeUtc,
        DateTime nowUtc, bool isPanelRequest, CancellationToken ctx);
}
