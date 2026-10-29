# 05 — İmza Doğrulama (kod kod)

**Dosyalar:**
`Application/Common/Abstractions/Services/IPaymentService.cs` (sözleşme)
`Infrastructure/Services/PaymentService.cs` (gerçek iş)

Buradaki kod, webhook'un **güvenlik kapısıdır**. Doğrulanmayan hiçbir istek handler'ın iş mantığına ulaşmaz.

---

## 5.1 Sözleşme — `IPaymentService`

```csharp
public interface IPaymentService
{
    Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, CheckoutRequestTypes requestType,
        CancellationToken ctx);

    // Sağlayıcıdan gelen webhook isteğinin imzasını doğrular ve olayı uygulama modeline çevirir.
    // İmza doğrulanamazsa BadRequestException fırlatılır (istek ödeme sağlayıcısından gelmemiştir).
    StripeWebhookEvent ParseWebhookEvent(string payload, string signature);
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `StripeWebhookEvent ParseWebhookEvent(string payload, string signature)` | "Ham gövde + imza ver, sana doğrulanmış olayı vereyim" sözleşmesi | Handler yalnızca bu arayüzü bilir; Stripe tiplerini **görmez** |
| Parametre **sırası** `(payload, signature)` | Önce gövde, sonra imza | Çağıran tarafın yanlış sırayla çağırması kolay bir hatadır; bu yüzden isimler ve sıra net tutuldu (`handler` içinde `request.Payload, request.Signature` şeklinde yazılıyor) |
| `BadRequestException` dokümantasyonu | Hata davranışı sözleşmede yazılı | Handler "başarısız olursa ne olur" bilgisini arayüzden öğrenir; uygulama değişse de kural aynı kalır |
| Application katmanında olması | Katman kuralı | Stripe SDK'sı `Infrastructure`'da. Application yalnızca arayüze ve kendi modeline bağlı kalır |

---

## 5.2 Girdi kontrolleri (en ucuz kapılar)

```csharp
public StripeWebhookEvent ParseWebhookEvent(string payload, string signature)
{
    if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
    {
        _logger.LogWarning(
            "Stripe webhook isteği eksik geldi: gövde veya Stripe-Signature başlığı boş.");

        throw new BadRequestException("Webhook gövdesi ve imzası zorunludur.");
    }

    // yapılandırma eksikliği iş kuralı hatası gibi görünmesin: sunucu tarafı hatasıdır (500)
    if (string.IsNullOrWhiteSpace(_stripeSettings.WebhookSecret))
    {
        _logger.LogCritical(
            "Stripe WebhookSecret yapılandırılmamış. User-secrets içinde 'Stripe:WebhookSecret' tanımlanmalıdır.");

        throw new InvalidOperationException(
            "Stripe yapılandırması eksik: 'Stripe:WebhookSecret' değeri bulunamadı.");
    }
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `IsNullOrWhiteSpace(payload) \|\| IsNullOrWhiteSpace(signature)` | Boş gövde/başlığı anında reddeder | Boş veriyle kripto hesabı yapmak anlamsız. Hata mesajı da net olur ("imzası doğrulanamadı" demek yerine "zorunludur") |
| `LogWarning` | Uyarı seviyesinde log | Bu, saldırı ya da istemci hatasıdır; **hata** değil **uyarı** olarak izlenir |
| `throw new BadRequestException(...)` | **400** döner | İstek biçimsel olarak eksik → istemci hatası |
| `IsNullOrWhiteSpace(_stripeSettings.WebhookSecret)` | Gizli anahtar tanımlı mı? | Anahtar yoksa imza **hiçbir zaman** doğrulanamaz; sistem çalışmaz |
| `LogCritical` | Kritik seviye log | Bu bir **sunucu yapılandırma** hatasıdır; operasyon ekibi anında görmeli |
| `throw new InvalidOperationException(...)` | **500** döner | İstemcinin düzeltebileceği bir şey değil. 400/422 dönmek "Stripe yanlış gönderdi" izlenimi verirdi — **yanlış teşhis** |
| Mesajda `'Stripe:WebhookSecret'` | Çözüm yolu mesajda | Hata mesajı operatörü doğru yere yönlendirir |

## 5.3 Asıl doğrulama — `EventUtility.ConstructEvent`

```csharp
Event stripeEvent;

try
{
    // İmza HMAC-SHA256 ile doğrulanır; zaman damgası toleransı SDK varsayılanıdır (300 saniye),
    // böylece eski imzaların tekrar oynatılması (replay) engellenir.
    // Hesap ile SDK API sürümü farklı olabileceği için sürüm uyuşmazlığı hata sayılmaz.
    stripeEvent = EventUtility.ConstructEvent(payload, signature, _stripeSettings.WebhookSecret,
        throwOnApiVersionMismatch: false);
}
catch (StripeException ex)
{
    // İmza geçersiz, gövde değiştirilmiş ya da zaman damgası tolerans dışı: istek sağlayıcıdan gelmemiştir.
    _logger.LogWarning(ex,
        "Stripe webhook imzası doğrulanamadı (istek reddedildi). Code:{Code}", ex.StripeError?.Code);

    throw new BadRequestException("Webhook imzası doğrulanamadı.");
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `EventUtility.ConstructEvent(payload, signature, secret, ...)` | 4 işi birlikte yapar: imza başlığını çözümler, `t=` zaman damgasını kontrol eder, HMAC-SHA256 ile imzayı yeniden hesaplar, geçerliyse JSON'u `Event` nesnesine çevirir | Kendi HMAC kodumuzu yazmak; zaman karşılaştırması ve hex çözümleme gibi hata yapmaya açık işleri elle yapmak demekti. Güvenlikte **"kendi kriptonu yazma"** ilkesi |
| `_stripeSettings.WebhookSecret` | Doğrulama anahtarı (yalnızca bizde + Stripe'ta) | Anahtar sızarsa saldırgan geçerli imza üretebilir → bu yüzden user-secrets/env'de tutulur, koda yazılmaz |
| `throwOnApiVersionMismatch: false` | Hesabın API sürümü ile SDK'nın beklediği sürüm farklıysa **hata vermez** | Bu bir **güvenlik sorunu değil**, sürüm uyumsuzluğudur. `true` kalsaydı Stripe sürümünü güncellediğinde tüm webhook'lar 400 olurdu |
| `try/catch (StripeException ex)` | Geçersiz imza, bozuk gövde, tolerans dışı zaman damgası — hepsi bu tipte fırlatılır | Tek yerde yakalamak, tüm "sahte/bozuk istek" ailesini aynı şekilde ele alır |
| `LogWarning(ex, ... Code:{Code})` | Uyarı + Stripe hata kodu | Saldırı denemesi ile entegrasyon hatası ayırt edilebilsin. **İstek gövdesi loglanmaz** (kişisel veri/sır olabilir) |
| `throw new BadRequestException("Webhook imzası doğrulanamadı.")` | **400** döner | İstek sağlayıcıdan gelmemiş demektir. Mesaj kısa tutulur: saldırgana ek bilgi verilmez |

### HMAC doğrulaması zihinde nasıl canlanır?

```text
Stripe tarafı:      HMAC_SHA256(secret, "t=1793000000." + gövde) → "5f3c9d..."  → başlığa koyar
Bizim tarafımız:    HMAC_SHA256(secret, "t=1793000000." + gövde) → "5f3c9d..." ?
                                                                  aynı ise: gerçek
                                                                  farklı ise: 400
Zaman kontrolü:     |şimdi - t| > 300 saniye  →  400 (replay koruması)
```

## 5.4 Olayı kendi modelimize çevirme

```csharp
StripeWebhookEventTypes eventType = MapEventType(stripeEvent.Type);

_logger.LogInformation("Stripe webhook olayı doğrulandı. EventId:{EventId}, EventType:{EventType}",
    stripeEvent.Id, stripeEvent.Type);

// olay verisi checkout oturumu değilse sipariş bilgisi çıkarılamaz (olay yoksayılır)
object? eventObject = stripeEvent.Data?.Object;

if (eventObject is not Session session)
{
    return new StripeWebhookEvent
    {
        EventId = stripeEvent.Id,
        EventType = eventType
    };
}

return new StripeWebhookEvent
{
    EventId = stripeEvent.Id,
    EventType = eventType,
    CheckoutSessionId = session.Id,
    OrderId = ResolveMetadataValue(session.Metadata, OrderIdMetadataKey),
    SessionPaymentStatus = session.PaymentStatus
};
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `MapEventType(stripeEvent.Type)` | Stripe'ın string olay adını **enum'a** çevirir | İş mantığı sağlayıcının yazım kurallarına bağlı kalmaz; karşılaştırmaları derleyici kontrol eder |
| `LogInformation("... doğrulandı")` | Başarılı doğrulamayı kaydeder | Şüpheli durumlarda "imza geçti mi?" bilgisi çok değerlidir |
| `stripeEvent.Data?.Object` | Olayın veri nesnesini alır | `?.` çünkü SDK beklenmedik bir yapı döndürebilir; çökmeyiz |
| `object? eventObject = ...` | Tip belirsizliğini kaldırır | SDK'nın `Object` alanı `object` tipindedir; önce ona alıp **sonra** tip kontrolü yapmak güvenli desendir |
| `if (eventObject is not Session session)` | Olay checkout oturumu **değilse** erken çıkar | İşleyemediğimiz veriyi zorla okumak çökme/yanlış eşleme riski. Sadece `EventId + EventType` döner → handler bunu "yoksay" olarak işler |
| `CheckoutSessionId = session.Id` | Ödeme oturumunun kimliği (`cs_...`) | Sipariş kaydındaki alanla eşleşir: **2. eşleme yolu** |
| `OrderId = ResolveMetadataValue(session.Metadata, OrderIdMetadataKey)` | Metadata'dan **bizim yazdığımız** `order_id` | En güvenilir eşleme bilgisi; üreticisi biziz |
| `SessionPaymentStatus = session.PaymentStatus` | `paid` / `unpaid` / `no_payment_required` | "Para gerçekten alındı mı?" kararının girdisi (bkz. 04.2) |

**Neden tüm Stripe olayı değil, sadece 5 alan?** İhtiyacımız olmayan veriyi içeri taşımak:
(1) gereksiz bağımlılık, (2) loglarda/hatalarda kişisel veri sızma riski, (3) Stripe alan değiştirdiğinde kırılma riski yaratır. Bu yüzden **dar bir sözleşme** kuruldu.

---

## 5.5 Yardımcılar

```csharp
private static StripeWebhookEventTypes MapEventType(string? stripeEventType)
{
    return stripeEventType switch
    {
        EventTypes.CheckoutSessionCompleted => StripeWebhookEventTypes.CheckoutSessionCompleted,
        EventTypes.CheckoutSessionAsyncPaymentSucceeded => StripeWebhookEventTypes.CheckoutSessionAsyncPaymentSucceeded,
        EventTypes.CheckoutSessionAsyncPaymentFailed => StripeWebhookEventTypes.CheckoutSessionAsyncPaymentFailed,
        EventTypes.CheckoutSessionExpired => StripeWebhookEventTypes.CheckoutSessionExpired,
        _ => StripeWebhookEventTypes.Unknown
    };
}

private static string? ResolveMetadataValue(Dictionary<string, string>? metadata, string key)
{
    if (metadata is null || !metadata.TryGetValue(key, out string? value))
    {
        return null;
    }

    return string.IsNullOrWhiteSpace(value) ? null : value;
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `EventTypes.CheckoutSessionCompleted` ... | Stripe SDK'sının **sabit** olay adları | Elle `"checkout.session.completed"` yazsaydık harf hatası sessizce "yoksay" davranışına dönüşürdü. Sabit kullanınca hata **derleme zamanında** çıkar |
| `_ => StripeWebhookEventTypes.Unknown` | Bilinmeyen olaylar | Yeni bir Stripe olayı çıksa sistem çökmez, "yoksay" olur (200 + `IGNORED`) |
| `metadata is null` kontrolü | SDK metadata'yı null döndürebilir | `NullReferenceException` riskini kapatır |
| `TryGetValue(key, out value)` | Anahtar var mı? (yoksa çökmez) | `order_id` yoksa `null` döneriz; handler oturum id ile eşlemeye geçer |
| `IsNullOrWhiteSpace(value) ? null : value` | Boş string'i `null`'a çevirir | "Boş" ve "yok" ayrımını ortadan kaldırır (oturum açılırken `user_id`/`table_id` boş string olarak yazılıyor) |
| `static` metotlar | Nesne durumu kullanmazlar | Saf fonksiyonlar: girdi → çıktı; okunması ve test edilmesi kolay |

---

## 5.6 Bu dosyadaki iki "altın" satır

| Satır | Neden altın |
| --- | --- |
| `EventUtility.ConstructEvent(payload, signature, secret, ...)` | Güvenlik kapısının kendisi. İmza doğrulanmadan hiçbir iş kuralı çalışmaz |
| `_stripeSettings.WebhookSecret` (koda gömülmemiş olması) | Sır yönetimi: anahtar koda yazılsaydı depoyu gören herkes geçerli imza üretebilirdi |

[→ 06 — Modeller, enum, Codes ve Swagger](06-modeller-enum-codes-swagger.md)