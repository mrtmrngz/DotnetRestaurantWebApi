# 06 — Modeller, Enum, `Codes`, Swagger ve Hata Eşlemesi

Webhook işinin "çevresindeki" küçük ama gerekli parçalar.

---

## 6.1 `StripeWebhookEvent` — handler'ın gördüğü model

**Dosya:** `Application/Models/Dtos/PaymentDtos/StripeWebhookEvent.cs`

```csharp
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
```

| Alan | Tip | Ne işe yarar | Neden bu tip |
| --- | --- | --- | --- |
| `EventId` | `string` | Log takibi (`evt_...`) | Hep dolu olur; `null!` ile "boş gelmeyecek" niyeti belirtildi |
| `EventType` | `StripeWebhookEventTypes` | Kararın anahtarı | Enum: derleyici kontrolü; varsayılan `Unknown` → bilinmeyen olayda güvenli davranış |
| `OrderId` | `string?` | Siparişi bulmanın 1. yolu | Metin: gövdeden gelen veri bozuk olabilir; dönüştürme işi handler'da (`Guid.TryParse`) |
| `CheckoutSessionId` | `string?` | Siparişi bulmanın 2. yolu + uyuşmazlık kontrolü | `?` çünkü olay oturum içermeyebilir |
| `SessionPaymentStatus` | `string?` | "Para alındı mı?" kararı | Ham string: Stripe yeni bir değer gönderirse karşılaştırma sessizce "hayır" olur — **güvenli varsayılan** |

**Neden `record` + `init`?** Olay verisi doğrulandıktan sonra **değişmemelidir**. Aksi hâlde "doğrulanan veri" ile "kullanılan veri" farklı olabilir (güvenlik açığı).

**Neden Stripe tipleri (`Event`, `Session`) burada yok?** Application katmanı Stripe kütüphanesini tanımaz. Sağlayıcı değişse bu model ve handler aynı kalır.

---

## 6.2 `StripeWebhookEventTypes` — olay isimlerinin enum hâli

**Dosya:** `Application/Common/Enums/StripeWebhookEventTypes.cs`

```csharp
public enum StripeWebhookEventTypes
{
    // İlgi alanımıza girmeyen ya da henüz desteklenmeyen olay.
    Unknown = 0,
    CheckoutSessionCompleted,
    CheckoutSessionAsyncPaymentSucceeded,
    CheckoutSessionAsyncPaymentFailed,
    CheckoutSessionExpired
}
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `Unknown = 0` | Bilinmeyen/ilgisiz olay | `0` verilmesi bilinçli: bir enum değeri **atanmazsa** varsayılan `0` = `Unknown` olur → güvenli varsayılan |
| `CheckoutSessionCompleted` | `checkout.session.completed` | Stripe string'i **Infrastructure'da** bu değere çevrilir; iş mantığı string görmez |
| `...AsyncPaymentSucceeded` | Asenkron ödeme başarılı | Kesinleşmiş başarı |
| `...AsyncPaymentFailed` | Asenkron ödeme başarısız | Kesinleşmiş başarısızlık |
| `...Expired` | Ödeme süresi doldu | Müşteri ödemedi |
| İngilizce isimler | Projedeki diğer enum'larla tutarlılık | Kodda terim birliği (Türkçe karşılıklar doküman/yorumda) |

**Neden string yerine enum?** Handler'da `stripeEvent.EventType == "checkout.session.completed"` yazsaydık: harf hatası **derleme zamanında yakalanmaz**, sessizce "yoksay" davranışına dönüşürdü. Enum ile hata **derleme anında** çıkar.

---

## 6.3 `Codes` enum'ına eklenen iki değer

**Dosya:** `Application/Common/Enums/Codes.cs`

```csharp
    CHECKOUT_SESSION_CREATED,
    CHECKOUT_SUCCESS,
    STRIPE_WEBHOOK_PROCESSED,   // ← eklendi
    STRIPE_WEBHOOK_IGNORED      // ← eklendi
}
```

| Değer | Ne zaman döner | Neden ayrı kod |
| --- | --- | --- |
| `STRIPE_WEBHOOK_PROCESSED` | Ödeme yazıldı / mükerrer olay / zaten sonuçlanmış | "İş tamam" anlamında tek kod; mesaj detayı `Message` alanında |
| `STRIPE_WEBHOOK_IGNORED` | İşlenmeyen veya henüz sonuçlanmamış olay | "Bu olayla ilgilenmedim" ile "işledim" **ayrı** görünsün; loglarda ve ileride alarm kurallarında ayrım gerekir |

**Neden enum'un sonuna eklendi?** Mevcut değerlerin numarası (implicit index) değişmesin diye yeni değerler **sona** eklenir; veritabanında/loglarda sayı olarak saklanan kodlar kaymasın.

## 6.4 Swagger örneği — `StripeWebhookProcessedResponseExample`

**Dosya:** `WebApi/Swagger/Examples/SuccessExamples/StripeWebhookProcessedResponseExample.cs`

```csharp
public class StripeWebhookProcessedResponseExample: IExamplesProvider<BaseResponse>
{
    public BaseResponse GetExamples()
    {
        return new BaseResponse
        {
            Message = "Ödeme onaylandı, sipariş ödendi olarak işaretlendi.",
            Code = Codes.STRIPE_WEBHOOK_PROCESSED
        };
    }
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `IExamplesProvider<BaseResponse>` | Swashbuckle'a "bu uç için örnek gövde şudur" der | Swagger UI'da elle yazılmış, gerçekçi bir örnek görünür |
| `Message` + `Code` | Gerçek cevabın birebir kopyası | Dokümantasyon ile davranışın **aynı** olması; yanıltıcı örnek, yanlış entegrasyon demektir |
| Controller'da kullanımı | `[SwaggerResponseExample(StatusCodes.Status200OK, typeof(StripeWebhookProcessedResponseExample))]` | 200 cevabının örneği bu sınıftan gelir |

---

## 6.5 Hata → HTTP kodu eşlemesi: `ExceptionMiddleware`

Webhook handler'ı hata durumunda **özel exception'lar** fırlatır; HTTP kodunu onlar belirlemez, tek bir middleware belirler.
**Dosya:** `WebApi/Middlewares/ExceptionMiddleware.cs`

| Exception | HTTP | `code` | Webhook'ta ne zaman |
| --- | --- | --- | --- |
| `ValidationException` (FluentValidation) | **400** | (alan listesi döner) | İmza/gövde boş ya da boyut aşımı |
| `BadRequestException` | **400** | `BAD_REQUEST` | İmza doğrulanamadı / gövde bozuk |
| `NotFoundException` | **404** | `NOT_FOUND` | Olayın siparişi bulunamadı |
| `ConflictException` | **409** | `CONFLICT` | Olayın oturumu siparişle eşleşmiyor |
| `InvalidOperationException` (AppException değil) | **500** | — | `Stripe:WebhookSecret` tanımsız (yapılandırma hatası) |
| Diğer tüm exception'lar | **500** | — | Beklenmeyen hata → `LogError` |

Middleware'in çekirdek mantığı:

```csharp
if (ex is AppException appException)
{
    statusCode = appException.StatusCode;   // 400 / 404 / 409 / 422 sınıfın kendisinden gelir
    code = appException.Code;               // Codes enum'ı
    logger.LogWarning(...);
}
else
{
    logger.LogError(...);                   // beklenmeyen hata
    statusCode = 500;
}
```

| Kod | Ne yapar | Neden önemli |
| --- | --- | --- |
| `ex is AppException` | İş kuralı hatalarını ayırır | Handler **HTTP kodu bilmez**; sadece "404'lük durum" der. Eşleme tek yerde |
| `appException.StatusCode` | Doğru kodu yazar | Tutarlılık: her handler aynı kuralı elle yazmak zorunda kalmaz |
| `logger.LogWarning` / `LogError` | Denetim izi | 4xx = beklenen iş durumu (uyarı), 5xx = sorun (hata) |

**Webhook açısından neden kritik?** Cevabın **kaç** olduğu Stripe'ın davranışını belirler: 2xx → "tekrar gönderme", 4xx/5xx → "tekrar gönder". Bu yüzden 404 (sipariş yok) bilinçli bir tercihtir: **sorun çözülene kadar Stripe denemeye devam eder.**

---

## 6.6 Cevap sözleşmesi (hepsi bir arada)

| Durum | HTTP | `code` | `message` |
| --- | --- | --- | --- |
| Ödeme yazıldı | 200 | `STRIPE_WEBHOOK_PROCESSED` | "Ödeme onaylandı, sipariş ödendi olarak işaretlendi." |
| İptal yazıldı | 200 | `STRIPE_WEBHOOK_PROCESSED` | "Ödeme başarısız olduğu için sipariş iptal edildi." |
| Mükerrer olay | 200 | `STRIPE_WEBHOOK_PROCESSED` | "Olay daha önce işlenmiş, sipariş durumu değiştirilmedi." |
| İşlenmeyen olay | 200 | `STRIPE_WEBHOOK_IGNORED` | "Olay işlenmedi, istek yoksayıldı." |
| Boş imza/gövde | 400 | (doğrulama listesi) | "Webhook imzası (Stripe-Signature) zorunludur." vb. |
| İmza geçersiz | 400 | `BAD_REQUEST` | "Webhook imzası doğrulanamadı." |
| Gövde bozuk | 400 | `BAD_REQUEST` | "Webhook gövdesi çözümlenemedi." |
| Sipariş yok | 404 | `NOT_FOUND` | "Webhook olayına ait sipariş bulunamadı." |
| Oturum çakışıyor | 409 | `CONFLICT` | "Webhook olayı siparişin ödeme oturumuyla eşleşmiyor." |
| Secret eksik | 500 | — | "Stripe yapılandırması eksik: 'Stripe:WebhookSecret' …" |

Bu tablo, **Stripe panelindeki webhook loglarını okurken** başvuracağın referanstır: orada HTTP kodu + zaman görünür; buradan hangi durumun oluştuğunu anlarsın.

[→ 07 — Testlerde webhook nasıl taklit edilir](07-testlerde-webhook-nasil-taklit-edilir.md)