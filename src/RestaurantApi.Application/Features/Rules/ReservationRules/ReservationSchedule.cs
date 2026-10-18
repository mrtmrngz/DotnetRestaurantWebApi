namespace RestaurantApi.Application.Features.Rules.ReservationRules;

// Rezervasyon takvimi.
// API'ye gelen ve doğrulanan tüm zamanlar YEREL saat (Avrupa/İstanbul, UTC+03:00, 24 saatlik) kabul edilir;
// yalnızca veritabanına yazılırken (timestamp with time zone) UTC karşılığına çevrilir.
public static class ReservationSchedule
{
    // Türkiye 2016'dan bu yana yaz saati uygulamadığı için sabit UTC+03:00 kullanılır.
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    // Restoran 7 gün açık: 10:00 - 24:00 (kapanış ertesi gün 00:00 kabul edilir).
    public static readonly TimeSpan OpeningTime = new(10, 0, 0);
    public static readonly TimeSpan ClosingTime = new(24, 0, 0);

    // Rezervasyon başlangıcı şuandan en az 1 saat sonrası olmalıdır.
    public static readonly TimeSpan MinStartTimeOffsetFromNow = TimeSpan.FromHours(1);

    // Rezervasyon başlangıcı restoran açılışından en az 1 saat sonrası olmalıdır (en erken 11:00).
    public static readonly TimeSpan MinStartTimeOffsetFromOpening = TimeSpan.FromHours(1);

    // Bir rezervasyonun süresi en fazla 3 saat olabilir.
    public static readonly TimeSpan MaxReservationDuration = TimeSpan.FromHours(3);

    // Rezervasyon başlangıcı kapanıştan en fazla 1.30 saat önce olabilir.
    public static readonly TimeSpan MaxStartTimeOffsetBeforeClosing = TimeSpan.FromMinutes(90);

    // Rezervasyon bitişi kapanıştan en fazla 30 dakika önce olabilir.
    public static readonly TimeSpan MaxEndTimeOffsetBeforeClosing = TimeSpan.FromMinutes(30);

    #region Conversion

    // "Şu an"ı yerel duvar saatine çevirir; tüm doğrulamalar yerel saatle yapılır.
    public static DateTime ToLocalTime(DateTime utcTime)
    {
        return utcTime + LocalOffset;
    }

    // İstemciden gelen yerel duvar saatini veritabanına yazılacak UTC değerine çevirir (Kind=Utc garanti edilir).
    public static DateTime ToUtcTime(DateTime localTime)
    {
        return DateTime.SpecifyKind(localTime - LocalOffset, DateTimeKind.Utc);
    }

    #endregion

    #region Sınır Hesapları

    // İlgili yerel günün açılış anı (10:00).
    public static DateTime GetDayOpening(DateTime localValue)
    {
        return localValue.Date + OpeningTime;
    }

    // İlgili yerel günün kapanış anı (ertesi gün 00:00).
    public static DateTime GetDayClosing(DateTime localValue)
    {
        return localValue.Date + ClosingTime;
    }

    // Rezervasyon başlangıcı için en erken an (11:00).
    public static DateTime GetEarliestStartTime(DateTime localValue)
    {
        return GetDayOpening(localValue) + MinStartTimeOffsetFromOpening;
    }

    // Rezervasyon başlangıcı için en geç an (22:30).
    public static DateTime GetLatestStartTime(DateTime localValue)
    {
        return GetDayClosing(localValue) - MaxStartTimeOffsetBeforeClosing;
    }

    // Rezervasyon bitişi için en geç an (23:30).
    public static DateTime GetLatestEndTime(DateTime localValue)
    {
        return GetDayClosing(localValue) - MaxEndTimeOffsetBeforeClosing;
    }

    #endregion
}
