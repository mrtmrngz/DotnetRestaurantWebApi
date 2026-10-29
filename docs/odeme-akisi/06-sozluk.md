# 06 — Sözlük

Ödeme ve bu projedeki mimari terimlerin sade karşılıkları. Alfabetik değil, **mantık sırasına** göre gruplandı.

---

## Ödeme dünyası

| Terim | Ne demek | Bu projede nerede | Benzetme |
| --- | --- | --- | --- |
| **Stripe** | Ödeme sağlayıcısı; kart işlemlerini bizim yerimize yapar | `Infrastructure/Services/PaymentService.cs` | Marketteki kasa |
| **Stripe Checkout** | Stripe'ın hazır ödeme sayfası ürünü | `SessionCreateOptions` | Kasanın ödeme ekranı |
| **Checkout Session (oturum)** | "Şu ürünleri şu tutarla ödet" talebinin karşılığı; bir ödeme denemesi | `CreateCheckoutSessionAsync` | Kasadaki fiş |
| **`sessionId` (`cs_...`)** | Oturumun kimlik numarası | `Order.CheckoutSessionId` | Fiş numarası |
| **`sessionUrl`** | Müşterinin gideceği güvenli ödeme adresi | `CheckoutResponse.SessionUrl` | Ödeme ekranına giden yol |
| **`status`** | Oturumun **sayfa** durumu (`open`, `complete`, `expired`) | Webhook gövdesinde (kullanmıyoruz) | Fiş açık mı, kapandı mı |
| **`payment_status`** | **Paranın** durumu (`paid`, `unpaid`, `no_payment_required`) | `StripeWebhookEvent.SessionPaymentStatus` | Para kasaya girdi mi |
| **Event (olay)** | Stripe'ın "şu oldu" bildirimi | `ProcessStripeWebhookCommand` | Kapı zili |
| **Event type** | Olayın türü (`checkout.session.completed` vb.) | `StripeWebhookEventTypes` | Zilin çeşidi (kargo/sipariş/misafir) |
| **Webhook** | Olay olduğunda Stripe'ın **bize** attığı HTTP isteği | `WebhooksController` | Kapı zili sistemi |
| **Webhook endpoint** | Bizim webhook adresimiz | `POST api/webhooks/checkout` | Zilin bağlı olduğu kapı |
| **Webhook secret (`whsec_...`)** | İmza üretmek/doğrulamak için gizli anahtar | `StripeSettings.WebhookSecret` | Kapıya özel mühür kalemi |
| **Signature / `Stripe-Signature`** | İsteğin gerçekten Stripe'tan geldiğinin kanıtı | Controller header + `ParseWebhookEvent` | Mühürlü zarf |
| **HMAC-SHA256** | İmza üretme yöntemi (tek yönlü özet) | Stripe SDK içinde | Mühür kalemi + özel desen |
| **`t=...` (timestamp)** | İmzanın üretildiği zaman | İmza başlığı içinde | Mührün tarihi |
| **`v1=...`** | İmzanın kendisi (hex) | İmza başlığı içinde | Mührün deseni |
| **Tolerance (tolerans)** | İmzanın kabul edildiği zaman penceresi (300 sn) | SDK varsayılanı | Mührün tazelik süresi |
| **Replay attack** | Eski imzalı isteği kopyalayıp tekrar gönderme | Tolerans + idempotency ile engellenir | Eski fişi tekrar kullanma |
| **Tampering** | Gövdeyi arada değiştirme | İmza tutmaz → 400 | Mektubu değiştirme |
| **Idempotency** | Aynı iş birden çok gelse sonucun bir kez oluşması | `X-Idempotency-Key` + `Pending` koşulu | Aynı fişi iki kez uzatmak |
| **`X-Idempotency-Key`** | İstemcinin ürettiği benzersiz istek anahtarı | Checkout uçlarının header'ı | İşlem numarası |
| **Metadata** | Oturuma iliştirdiğimiz kendi notlarımız (`order_id` vb.) | `BuildMetadata` | Koliye yazılan takip no |
| **Minor unit (kuruş)** | Para biriminin en küçük birimi; Stripe tam sayı bekler | `ToMinorUnits` | Parayı kuruşla saymak |
| **3D Secure** | Bankanın ek doğrulama adımı | Stripe sayfasında olur, kodumuzda yok | Kart şifresi SMS'i |
| **Async payment** | Sonucu anında belli olmayan ödeme yöntemi | `async_payment_succeeded/failed` | Havale: para yarın geçer |
| **Refund / Chargeback** | İade / itiraz | **Kapsamda değil** (bkz. [04](04-guvenlik-ve-tasarim-kararlari.md) §7) | Parayı geri verme |
| **PCI-DSS** | Kart verisiyle çalışma standardı | Kart verisi hiç dokunulmadığı için kapsam dışı | Kasa güvenlik sertifikası |
| **Test mode / Live mode** | Stripe'ın deneme ve gerçek ortamları | Test anahtarları `sk_test_`, `whsec_test_` | Prova seansı / gerçek oyun |
| **PaymentIntent** | Stripe'ın "ödeme niyeti" nesnesi | **Kullanmıyoruz:** Checkout oturumu bize yetiyor | Kasadaki işlem kaydı |

---

## Proje mimarisi

| Terim | Ne demek | Bu projede | Neden var |
| --- | --- | --- | --- |
| **Domain** | İş kurallarının saf hâli; hiçbir teknolojiye bağlı değil | `Order`, `OrderItem`, `OrderStatus`, `PaymentStatus` | Kuralı tek yerde tutmak; veritabanı/HTTP değişse kural aynı kalır |
| **Application** | Kullanım senaryoları (iş akışları) | `Features/Checkout/Commands/...` | "Sipariş nasıl açılır?" sorusunun cevabı burada |
| **Infrastructure** | Dış dünya: Stripe, Redis, S3, mail | `PaymentService`, `CartService` | Dış bağımlılığı içeri sokmamak |
| **WebApi** | HTTP dünyası: controller, middleware | `CheckoutController`, `WebhooksController` | Header/route/status kodları |
| **CQRS** | Yazma ve okuma işlerini ayırma yaklaşımı | `Commands/` (yazma), `Queries/` (okuma) | Yazma akışına kilit/idempotency gibi kurallar eklenirken okumayı yavaşlatmamak |
| **MediatR** | İsteği doğru handler'a taşıyan araç | `_mediator.Send(command)` | Controller iş mantığını bilmez |
| **Command** | "Şunu yap" mesajı + verisi | `CreateOnlineCheckoutCommand`, `ProcessStripeWebhookCommand` | Katmanlar arası veri sözleşmesi |
| **Handler** | Komutu işleyen kod | `...CommandHandler.Handle(...)` | Akışın kalbi |
| **Validator (FluentValidation)** | İsteğin biçimsel kontrolü | `...CommandValidator.cs` | Bozuk istek iş mantığına ulaşmasın |
| **Pipeline Behavior** | Handler'dan önce/sonra çalışan ara katmanlar | `ValidationBehavior`, `LoggingBehavior` | Kuralları her handler'da tekrar yazmamak |
| **`IPaymentService`** | Ödeme sağlayıcısı **arayüzü** | `Application/Common/Abstractions/Services` | Application'ın Stripe'ı tanımaması; testlerde sahtelenebilmesi |
| **Repository / `IUnitOfWork`** | Veri erişimi soyutlaması + kaydetme birimi | `IOrderRepository`, `IUnitOfWork` | Veritabanı teknolojisini gizlemek |
| **`ExecuteUpdateAsync` / `ExecuteDeleteAsync`** | Kaydı çekmeden tek SQL ile güncelleme/silme | Webhook, sepet temizliği | Daha az sorgu, daha az yarış riski |
| **Global query filter** | EF Core'un silinmiş kaydı otomatik gizlemesi | `Product.IsDeleted` | Silinmiş ürün satışta görünmesin. **İstisna:** sepet kontrolünde bilinçli kapatılır (`IgnoreQueryFilters`) |
| **Snapshot (ProductSnapshot / AddressSnapshot)** | Sipariş anındaki ürün/adres bilgisinin JSON kopyası | `Order.CreateFromCart`, `HandleSetAddress` | Geçmiş bozulmasın; ürün/adres sonradan değişse bile |
| **Redis cache** | Sık okunan verilerin hızlı kopyası | `InvalidateCartCache` | Hız. Mutasyon sonrası **temizlenmezse** eski veri görünür |
| **`AppException`** | İş kuralı hatası üst tipi (404/409/422/400 kodlarını taşır) | `Common/Exceptions/AppException.cs` | HTTP kodunu iş katmanında belirlemek |
| **`ExceptionMiddleware`** | Hatayı JSON cevabına çeviren ara katman | `WebApi/Middlewares` | Tek yerde tutarlı hata formatı |
| **`Codes` enum'ı** | Cevaplarda dönen iş kodları | `CHECKOUT_SESSION_CREATED`, `STRIPE_WEBHOOK_PROCESSED` | İstemci mesaj metnine bağımlı olmasın |

---

## HTTP / dayanıklılık

| Terim | Ne demek | Bu akıştaki karşılığı |
| --- | --- | --- |
| **2xx** | "İş tamam, tekrar gönderme" | Stripe'ın retry'ını durdurur → işlenmeyen olayda 200 |
| **4xx** | "İstekte sorun var" | İmza hataları (400), sipariş yok (404), çakışma (409) → Stripe **tekrar dener** |
| **5xx** | "Sunucuda sorun var" | Eksik `Stripe:WebhookSecret` (500) |
| **Retry (tekrar deneme)** | Stripe'ın başarısız webhook'u yeniden göndermesi | 2xx verdiğimiz sürece tekrar yok |
| **Race condition (yarış)** | İki isteğin aynı kayda aynı anda yazması | Koşullu tek SQL + `X-Idempotency-Key` ile engellenir |
| **Fail-fast** | Hata varsa sessizce devam etmek yerine hemen hata vermek | Sipariş bulunamazsa 404 + `LogError` |
| **`[AllowAnonymous]`** | Ucun token istememesi | Webhook ucu (kimlik doğrulaması imzayla) |
| **`CancellationToken`** | İşlem iptal edildiğinde durma sinyali | İstemci koparsa gereksiz DB yazımı olmaz |

---

## Sık karıştırılan üç şey

1. **`status` ≠ `payment_status`.** İlki sayfanın, ikincisi paranın durumu. Bizim kararımız **paraya** bakar.
2. **Webhook ≠ SuccessUrl dönüşü.** Webhook Stripe'tan gelir (kanıt), dönüş ise müşterinin tarayıcısından gelir (kanıt değil).
3. **Sipariş oluştu ≠ para alındı.** Sipariş `Pending` doğar; `Paid` olması için kesinleşmiş bir ödeme olayı gerekir.

---

[← Başa dön: README](README.md)