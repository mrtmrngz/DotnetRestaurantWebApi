# 03 — `ProcessStripeWebhookCommandHandler.Handle` (blok blok)

**Dosya:** `Application/Features/Checkout/Commands/ProcessStripeWebhookCommand/ProcessStripeWebhookCommandHandler.cs`
**Görev:** Webhook'un "beynidir". İmzayı doğrular, olayı yorumlar, siparişi bulur, günceller, cevap üretir.

`Handle` metodunda **4 numaralı adım** vardır ve kodda da `(1) (2) (3) (4)` diye işaretlenmiştir.

---

## 3.1 Sınıfın hazırlık kısmı

```csharp
public class ProcessStripeWebhookCommandHandler: IRequestHandler<ProcessStripeWebhookCommand, BaseResponse>
{
    private const string PaidSessionPaymentStatus = "paid";
    private const string NoPaymentRequiredSessionPaymentStatus = "no_payment_required";

    private static readonly OrderTransition FailedOrderTransition =
        new(PaymentStatus.Failed, OrderStatus.Canceled);

    private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;

    public ProcessStripeWebhookCommandHandler(ILogger<ProcessStripeWebhookCommandHandler> logger,
        IPaymentService paymentService, IOrderRepository orderRepository)
    {
        _logger = logger;
        _paymentService = paymentService;
        _orderRepository = orderRepository;
    }
```

| Kod | Ne yapar | Neden gerekli |
| --- | --- | --- |
| `IRequestHandler<Command, BaseResponse>` | "Bu komutu işleyen sınıf benim" sözleşmesi | MediatR handler'ı otomatik bulur; DI kaydı **gerekmez** |
| `PaidSessionPaymentStatus = "paid"` | Stripe'ın "para alındı" değeri | Sihirli string'i tek yerde tutar; yazım hatası riski azalır |
| `NoPaymentRequiredSessionPaymentStatus = "no_payment_required"` | "Ödeme gerekmiyor" (0 TL'lik oturum) | Bu da **başarılı** sayılmalı; aksi hâlde bedava sipariş `Pending` kalırdı |
| `FailedOrderTransition = (Failed, Canceled)` | Başarısız ödemenin tek, ortak sonucu | Aynı geçiş iki olayda kullanıldığı için bir kez tanımlandı; değişirse tek yerden değişir |
| `_logger` | İz kaydı | Webhook'un tek gözü bu loglar; operatör buradan teşhis yapar |
| `_paymentService` | Ödeme sağlayıcısı **arayüzü** | İmza doğrulama + olay çözümleme (Stripe detayı Infrastructure'da kalır) |
| `_orderRepository` | Siparişe erişim | Siparişi bulmak ve güncellemek için |

**Dikkat:** Handler'da `IUnitOfWork` **yok**. Çünkü güncelleme `ExecuteUpdateAsync` ile doğrudan SQL'e yapılır (bkz. 04) — kaydetmeye gerek kalmaz.

---

## 3.2 Adım (1): imza doğrulaması + olayın modele çevrilmesi

```csharp
// (1) imza doğrulaması yapılır ve sağlayıcı olayı uygulama modeline çevrilir
StripeWebhookEvent stripeEvent = _paymentService.ParseWebhookEvent(request.Payload, request.Signature);

_logger.LogInformation("Stripe webhook olayı alındı. EventId:{EventId}, EventType:{EventType}",
    stripeEvent.EventId, stripeEvent.EventType);
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `ParseWebhookEvent(request.Payload, request.Signature)` | İmzayı doğrular, olayı 5 alanlı kendi modelimize çevirir | Tek çağrıda iki iş: **güvenlik** ve **veri çıkarma**. Sıra önemli: **payload önce, signature sonra** (metodun imzası `(payload, signature)`) |
| Dönüş ataması `StripeWebhookEvent` | Artık elimizde sağlayıcıdan bağımsız bir nesne var | Handler'ın geri kalanı Stripe tiplerini **hiç görmez** |
| `LogInformation("... EventId:{EventId}, EventType:{EventType}")` | Olayın kimliğini ve tipini loglar | Yapılandırılmış log (Serilog): aynı `EventId` ile tüm akış izlenebilir. **Gövde loglanmaz** (kişisel veri olabilir) |

**Bu satır patlarsa ne olur?**

| Durum | Fırlatılan | HTTP |
| --- | --- | --- |
| Gövde/imza boş | `BadRequestException` | 400 |
| İmza geçersiz / eski / gövde bozulmuş | `BadRequestException` | 400 |
| `Stripe:WebhookSecret` tanımsız | `InvalidOperationException` (config hatası) | 500 |

Yani **imza doğrulanmadan aşağıdaki hiçbir satır çalışmaz** — akışın güvenlik kapısı burasıdır.

---

## 3.3 Adım (2): olayın karara çevrilmesi (ve yoksayma)

```csharp
// (2) olayın siparişe yansıması belirlenir; işlenmeyen olaylar yoksayılır ve 200 döner
//     (Stripe yalnızca 2xx dışı yanıtlarda olayı tekrar gönderir).
OrderTransition? transition = ResolveTransition(stripeEvent);

if (transition is null)
{
    _logger.LogInformation(
        "Webhook olayı sipariş durumunu değiştirmedi. EventId:{EventId}, EventType:{EventType}",
        stripeEvent.EventId, stripeEvent.EventType);

    return new BaseResponse
    {
        Code = Codes.STRIPE_WEBHOOK_IGNORED,
        Message = "Olay işlenmedi, istek yoksayıldı."
    };
}
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `ResolveTransition(stripeEvent)` | Olay tipini + `payment_status`'u "sipariş ne olacak" kararına çevirir | Karar mantığı tek bir yerde (bkz. 04). Handler sadece sonucu uygular |
| `OrderTransition?` (nullable) | `null` = "bu olayla yapılacak bir şey yok" | "Yoksay" durumunu hata fırlatmadan, açıkça ifade eder |
| `return ... Codes.STRIPE_WEBHOOK_IGNORED` | **200** döner + "yoksayıldı" kodu | Stripe 2xx görürse **tekrar göndermez**. İlgilenmediğimiz olay için günlerce retry istemeyiz |
| Log: "sipariş durumunu değiştirmedi" | Görünürlük | "Neden hiçbir şey olmadı?" sorusunun cevabı logda durur |

**Ne zaman `null` döner?** (detaylar 04'te)

- Bilmediğimiz olay tipleri (`payment_intent.created` vb.),
- `checkout.session.completed` **ama** `payment_status` hâlâ "para alınmadı" diyor (asenkron ödeme henüz sonuçlanmadı → sonuç ayrı olayla gelecek).

## 3.4 Adım (3): siparişi bulma

```csharp
// (3) olayın ait olduğu sipariş bulunur
Guid orderId = await ResolveOrderIdAsync(stripeEvent, cancellationToken);
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `ResolveOrderIdAsync(...)` | Olayı bir siparişe eşler; bulamazsa `NotFoundException` (404), çakışma varsa `ConflictException` (409) | Ayrıntısı 04'te. Burada tek satır olması bilinçli: Handle metodu "okunabilir bir hikâye" olsun diye karmaşık parçalar yardımcı metotlara taşındı |

**Neden 404 dönmek "kötü" değil, aksine doğru?**
Sessizce 200 dönseydik Stripe "işlendi" sanır ve **bir daha denemezdi**; ödeme alınmış ama sipariş güncellenmemiş olarak kalırdı. 404 dönünce Stripe tekrar dener ve olay kaybolmaz.

---

## 3.5 Adım (4): atomik güncelleme + idempotency

```csharp
// (4) sipariş atomik olarak güncellenir; zaten sonuçlanmışsa hiçbir şey yazılmaz (idempotent)
int affectedRowCount = await ApplyTransitionAsync(orderId, transition.Value, cancellationToken);

if (affectedRowCount == 0)
{
    _logger.LogInformation(
        "Sipariş daha önce sonuçlanmış, güncelleme yapılmadı. OrderId:{OrderId}, EventId:{EventId}",
        orderId, stripeEvent.EventId);

    return new BaseResponse
    {
        Code = Codes.STRIPE_WEBHOOK_PROCESSED,
        Message = "Olay daha önce işlenmiş, sipariş durumu değiştirilmedi."
    };
}

_logger.LogInformation(
    "Sipariş ödeme durumu webhook ile güncellendi. OrderId:{OrderId}, PaymentStatus:{PaymentStatus}, OrderStatus:{OrderStatus}",
    orderId, transition.Value.PaymentStatus, transition.Value.OrderStatus);

return new BaseResponse
{
    Code = Codes.STRIPE_WEBHOOK_PROCESSED,
    Message = transition.Value.Succeeded
        ? "Ödeme onaylandı, sipariş ödendi olarak işaretlendi."
        : "Ödeme başarısız olduğu için sipariş iptal edildi."
};
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `ApplyTransitionAsync(...)` | **Tek SQL** ile siparişi günceller; yalnızca `PaymentStatus == Pending` olan satırı hedefler | Idempotency ve "sonuçlanmış ödemeyi geri alma" koruması buradadır |
| `int affectedRowCount` | Kaç satır güncellendi bilgisini alır | 0 satır = "zaten sonuçlanmış" demektir; bunu **ayırt etmek** isteriz (aksi hâlde "işledim" diye yalan söylemiş oluruz) |
| `if (affectedRowCount == 0)` bloğu | Mükerrer olayı **başarı** olarak raporlar | İstenen sonuç zaten sağlanmış → 200 + "daha önce işlenmiş". Böylece Stripe retry yapmaz, log da temiz kalır |
| `_logger.LogInformation(... PaymentStatus, OrderStatus)` | Ne yazıldığını kaydeder | Para akışında **denetim izi** şart |
| `transition.Value.Succeeded ? ... : ...` | Mesajı sonuca göre seçer | Kullanıcıya/operasyona "ödendi" mi "iptal" mi olduğunu net söyler |
| `Codes.STRIPE_WEBHOOK_PROCESSED` | İş kodu | İstemci/panel mesaj metnine göre değil **koda göre** karar verir |

---

## 3.6 `Handle`'ın tam izi (örnek: başarılı ödeme)

| # | Satır | Sonuç |
| --- | --- | --- |
| — | *(validator)* imza/gövde dolu mu? | ✅ |
| 1 | `ParseWebhookEvent` | İmza geçerli → `EventType=CheckoutSessionCompleted`, `SessionPaymentStatus="paid"`, `OrderId=<guid>` |
| 2 | `ResolveTransition` | `CheckoutSessionCompleted` + paid → `OrderTransition(Paid, null)` |
| 3 | `ResolveOrderIdAsync` | Sipariş bulundu (metadata `order_id`), oturum uyuşuyor → `orderId` |
| 4 | `ApplyTransitionAsync` | `UPDATE ... WHERE PaymentStatus=Pending` → **1 satır** |
| — | Cevap | 200, `STRIPE_WEBHOOK_PROCESSED`, "Ödeme onaylandı, sipariş ödendi olarak işaretlendi." |

Aynı olay ikinci kez gelirse: adım 1-3 aynı, adım 4'te **0 satır** → 200 + "Olay daha önce işlenmiş…" (hiçbir şey bozulmaz).

[→ 04 — Yardımcı metotlar](04-handler-yardimci-metotlar.md)