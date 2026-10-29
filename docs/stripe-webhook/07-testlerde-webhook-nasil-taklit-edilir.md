# 07 — Testlerde Webhook Nasıl Taklit Edilir?

**Dosya:** `tests/RestaurantApi.IntegrationTests/Setup/FakePaymentService.cs` (webhook kısmı)

Entegrasyon testleri **gerçek Stripe'a çıkmaz**. Testte `IPaymentService` yerine bu sahte (fake) servis kullanılır:

```csharp
// Fixture içinde: IPaymentService kaydı kaldırılır, yerine FakePaymentService eklenir.
services.AddScoped<IPaymentService, FakePaymentService>();
```

Neden? Testler **ağ çağrısı yapmadan**, **deterministik** ve **hızlı** çalışmalıdır. Gerçek Stripe'a çıkmak; anahtar gerektirir, internet gerektirir, sonuç değişebilir.

---

## 7.1 İmza doğrulamasının taklidi

```csharp
public const string ValidSignature = "t=1,v1=fake_valid_signature";
public const string InvalidSignature = "t=1,v1=fake_invalid_signature";

public StripeWebhookEvent ParseWebhookEvent(string payload, string signature)
{
    if (string.IsNullOrWhiteSpace(signature) || signature == InvalidSignature)
    {
        throw new BadRequestException("Webhook imzası doğrulanamadı.");
    }
    ...
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `ValidSignature` / `InvalidSignature` sabitleri | Testlerin kullanacağı iki senaryo anahtarı | Test kodu **sihirli string** yazmaz; niyeti okunur olur |
| `IsNullOrWhiteSpace(signature)` | Boş imzayı reddeder | Gerçek servisle **aynı davranış** (400) |
| `signature == InvalidSignature` | `"v1=fake_invalid_signature"` gönderilirse 400 | Test, "imza geçersiz" senaryosunu bu sabitle tetikler |
| `throw new BadRequestException(...)` | **Gerçek servisle aynı exception ve mesaj** | Testte gördüğümüz davranış, üretimdekiyle birebir aynı olmalı. Aksi hâlde testler yalancı güven verir |

> **Not:** Gerçek `EventUtility.ConstructEvent` çağrısı ve HMAC doğrulaması bu fake'te **yapılmaz**; onu unit test tarafı gerçek `PaymentService` ile doğrular (test, gizli anahtarla kendi HMAC-SHA256 imzasını üretip geçerli/geçersiz/eskimiş senaryoları çalıştırır).
> Böylece: **imza matematiği** → unit test; **iş akışı** → entegrasyon testi (fake ile).

---

## 7.2 Olay gövdesinin taklidi (gerçek Stripe alanları okunur)

```csharp
try
{
    using JsonDocument document = JsonDocument.Parse(payload);

    JsonElement root = document.RootElement;

    JsonElement session = default;

    bool hasSession = root.TryGetProperty("data", out JsonElement data)
                      && data.TryGetProperty("object", out session);

    return new StripeWebhookEvent
    {
        EventId = GetStringValue(root, "id") ?? string.Empty,
        EventType = MapEventType(GetStringValue(root, "type")),
        CheckoutSessionId = hasSession ? GetStringValue(session, "id") : null,
        OrderId = hasSession ? GetMetadataValue(session, "order_id") : null,
        SessionPaymentStatus = hasSession ? GetStringValue(session, "payment_status") : null
    };
}
catch (JsonException)
{
    throw new BadRequestException("Webhook gövdesi çözümlenemedi.");
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `JsonDocument.Parse(payload)` | Testin gönderdiği gövdeyi çözümler | Testler **gerçek Stripe olayı JSON'u** gönderir; fake onu gerçek servisle aynı alanlardan okur (`type`, `data.object.id`, `metadata.order_id`, `payment_status`) |
| `TryGetProperty("data")` → `"object"` | Oturum nesnesine iner | Test gövdesi gerçek yapıyı taklit eder; fake de aynı yolu izler |
| `session = default` + `out session` | Değişkeni önceden başlatır | Kısa devre nedeniyle `out` atanmamış kalabilirdi; derleyici hatası bu şekilde kapatılır |
| `MapEventType(...)` (fake içindeki kopya) | String olay adını enum'a çevirir | Fake kendi içinde basit bir eşleme tutar (gerçek servis SDK sabitlerini kullanır); testlerin istediği senaryo buradan üretilir |
| `?? string.Empty` | `EventId` boşsa bile null bırakmaz | `EventId` non-nullable; log satırları çökmesin |
| `catch (JsonException)` → `BadRequestException` | Bozuk gövdede 400 | Gerçek servis de bozuk gövdede 400 döner (SDK `StripeException` fırlatır) |

### Testler bu fake'i nasıl kullanıyor?

```csharp
// Geçerli imza + gerçek Stripe olayı → handler akışı gerçekten çalışır
request.Headers.Add("Stripe-Signature", FakePaymentService.ValidSignature);

// Geçersiz imza → 400 beklenir, veritabanı değişmemeli
request.Headers.Add("Stripe-Signature", FakePaymentService.InvalidSignature);
```

Gövde şablonu testte şöyle kurulur (yer tutucular doldurulur):

```csharp
private const string PayloadTemplate =
    "{\"id\":\"evt_test_1\",\"object\":\"event\",\"type\":\"__TYPE__\",\"data\":{\"object\":{\"id\":\"__SESSION__\",\"object\":\"checkout.session\",\"payment_status\":\"__PAYMENT_STATUS__\",\"metadata\":{\"order_id\":\"__ORDER__\"}}}}";
```

| Parça | Açıklama |
| --- | --- |
| `"type"` | Hangi olay simüle ediliyor (`checkout.session.completed`, `checkout.session.expired`, …) |
| `"data.object.id"` | Oturum id'si → **2. eşleme yolu** test edilir |
| `"data.object.metadata.order_id"` | Sipariş id'si → **1. eşleme yolu** test edilir |
| `"data.object.payment_status"` | `paid`/`unpaid` → "para alındı mı" kararı test edilir |

---

## 7.3 Neden bu kadar önemli?

| Test türü | Neyi doğrular | Gerçek Stripe'a çıkar mı? |
| --- | --- | --- |
| Unit (`PaymentServiceWebhookParsingTests`) | İmza matematığı (HMAC), tolerans dışı imza, bozuk gövde, `WebhookSecret` eksikliği | ❌ Hayır (imza yerelde üretilir) |
| Entegrasyon (`StripeWebhookEndpointTests`) | Uç → handler → veritabanı zinciri: `Paid`, `Failed+Canceled`, 400/404, idempotency | ❌ Hayır (fake ile) |
| Gerçek Stripe | Uçtan uca gerçek olay | ✅ Sadece manuel/`stripe trigger` ile |

Yani **kodun doğruluğu** fake ile, **güvenliğin doğruluğu** (kripto) ise gerçek servisle ayrı ayrı kanıtlanır. Bu ikisini karıştırmamak, testlerin yalancı güven vermesini engeller.

---

[← README](README.md) · [← 06 — Modeller ve cevap sözleşmesi](06-modeller-enum-codes-swagger.md)