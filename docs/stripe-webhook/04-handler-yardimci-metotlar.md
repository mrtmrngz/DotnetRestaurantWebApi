# 04 — Handler'ın Yardımcı Metotları (blok blok)

Bu dosya `ProcessStripeWebhookCommandHandler` içindeki 4 yardımcı metodu ve 1 küçük tipi anlatır. `Handle` metodunu "okunabilir bir hikâye" tutmak için karmaşık parçalar buraya taşındı.

---

## 4.1 `ResolveTransition` — olay → karar

```csharp
private static OrderTransition? ResolveTransition(StripeWebhookEvent stripeEvent)
{
    return stripeEvent.EventType switch
    {
        // Senkron ödeme: oturum tamamlandı ancak ödemenin alındığı oturumun payment_status alanından doğrulanır
        // (asenkron yöntemlerde oturum "completed" olsa bile ödeme henüz sonuçlanmamış olabilir).
        StripeWebhookEventTypes.CheckoutSessionCompleted when IsSessionPaid(stripeEvent) =>
            new OrderTransition(PaymentStatus.Paid, null),

        // Asenkron ödeme sonucu ayrı olay ile kesinleşir.
        StripeWebhookEventTypes.CheckoutSessionAsyncPaymentSucceeded =>
            new OrderTransition(PaymentStatus.Paid, null),

        // Ödeme başarısız oldu ya da oturumun süresi doldu: sipariş iptal edilir.
        StripeWebhookEventTypes.CheckoutSessionAsyncPaymentFailed => FailedOrderTransition,
        StripeWebhookEventTypes.CheckoutSessionExpired => FailedOrderTransition,

        _ => null
    };
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `static` metot | Nesne durumuna ihtiyaç duymaz | Sadece giriş → çıkış hesabı yapar; yan etkisi yok, test etmesi kolay |
| `switch` expression | Olay tipine göre karar verir | 4 satırlık okunabilir bir karar tablosu; `if/else` zincirinden daha net |
| `when IsSessionPaid(stripeEvent)` | **Ek koşul**: "`.completed` geldi **ve** para gerçekten alındıysa" | Asenkron ödeme yöntemlerinde oturum "completed" olup `payment_status` hâlâ "unpaid" olabilir. Bu koşul olmasaydı **ödenmemiş sipariş `Paid` yazılırdı** (para kaybı) |
| `new OrderTransition(PaymentStatus.Paid, null)` | Sonuç: ödeme `Paid`; sipariş durumuna **dokunma** (`null`) | Siparişin mutfak akışı (Pending→Confirmed→…) ayrı bir iştir; ödeme onu değiştirmemeli |
| `FailedOrderTransition` | `(PaymentStatus.Failed, OrderStatus.Canceled)` | Başarısız/expired ödeme → sipariş de iptal. Ödeme alınmadan üretime girmesin |
| `_ => null` | Eşleşmeyen her olay → "yapılacak iş yok" | İlgilenmediğimiz olaylarda 200 döneriz (bkz. 03 adım 2) |

### Karar tablosu (kodun özeti)

| Olay | Ek koşul | Karar | Sipariş sonucu |
| --- | --- | --- | --- |
| `CheckoutSessionCompleted` | `payment_status` ∈ {`paid`, `no_payment_required`} | `Paid`, sipariş durumu aynı | `PaymentStatus = Paid` |
| `CheckoutSessionCompleted` | aksi hâlde | `null` (yoksay) | değişmez |
| `CheckoutSessionAsyncPaymentSucceeded` | — | `Paid`, sipariş durumu aynı | `PaymentStatus = Paid` |
| `CheckoutSessionAsyncPaymentFailed` | — | `Failed` + `Canceled` | `PaymentStatus = Failed`, `OrderStatus = Canceled` |
| `CheckoutSessionExpired` | — | `Failed` + `Canceled` | Aynı |
| Diğer | — | `null` (yoksay) | değişmez |

---

## 4.2 `IsSessionPaid` — "para gerçekten alındı mı?"

```csharp
private static bool IsSessionPaid(StripeWebhookEvent stripeEvent)
{
    return string.Equals(stripeEvent.SessionPaymentStatus, PaidSessionPaymentStatus,
               StringComparison.OrdinalIgnoreCase)
           || string.Equals(stripeEvent.SessionPaymentStatus, NoPaymentRequiredSessionPaymentStatus,
               StringComparison.OrdinalIgnoreCase);
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `string.Equals(a, b, ...)` | Null-güvenli karşılaştırma | `SessionPaymentStatus` null olabilir (olay oturum içermiyorsa). `==` de null-güvenlidir ama burada **bilinçli olarak harf büyüklüğünden bağımsız** karşılaştırma istiyoruz |
| `StringComparison.OrdinalIgnoreCase` | Harf büyüklüğünü yok sayar | Stripe değerleri küçük harftir; yine de kültürden bağımsız (Ordinal) karşılaştırmak, Türkçe `i/İ` gibi kültür tuzaklarını önler |
| `|| NoPaymentRequired...` | 0 TL'lik oturumları da "ödendi" sayar | "Ödeme gerekmiyor" = tahsil edilecek bir şey yok. Bu durumu yoksaymak, bedava siparişin sonsuza kadar `Pending` kalmasına yol açardı |
| İki ayrı `string.Equals` | Okunabilirlik | Tek satırda `is "paid" or "no_payment_required"` de yazılabilirdi; bu hâliyle **hangi değerlerin kabul edildiği** daha açık |

> **Güvenlik notu:** Buradaki değer **beyaz liste** mantığıdır: yalnızca bilinen "para alındı" değerleri geçer. Stripe yeni bir değer gönderirse (ör. `processing`) sistem "ödendi" **demez**, bekler. Yanlış pozitif = bedava sipariş riski olduğu için "şüphede kalınca ödendi sayma" kuralı uygulanır.

## 4.3 `ResolveOrderIdAsync` — olayı doğru siparişe bağlama

```csharp
private async Task<Guid> ResolveOrderIdAsync(StripeWebhookEvent stripeEvent, CancellationToken cancellationToken)
{
    Guid? orderIdFromMetadata = Guid.TryParse(stripeEvent.OrderId, out Guid parsedOrderId)
        ? parsedOrderId
        : null;

    string? sessionId = string.IsNullOrWhiteSpace(stripeEvent.CheckoutSessionId)
        ? null
        : stripeEvent.CheckoutSessionId;

    // sipariş önce oturum açılırken metadata'ya yazdığımız order_id ile, yoksa sipariş kaydındaki
    // checkout session id ile bulunur
    var order = await _orderRepository.GetAllAsQueryable()
        .AsNoTracking()
        .Where(x => (orderIdFromMetadata.HasValue && x.Id == orderIdFromMetadata.Value)
                    || (sessionId != null && x.CheckoutSessionId == sessionId))
        .Select(x => new { x.Id, x.CheckoutSessionId })
        .FirstOrDefaultAsync(cancellationToken);

    if (order is null)
    {
        // Sessizce yutulmaz: Stripe 2xx dışı yanıtlarda olayı tekrar gönderir, ödeme kaydı kaybolmaz.
        _logger.LogError(
            "Webhook olayına ait sipariş bulunamadı. EventId:{EventId}, EventType:{EventType}, SessionId:{SessionId}, OrderId:{OrderId}",
            stripeEvent.EventId, stripeEvent.EventType, sessionId, stripeEvent.OrderId);

        throw new NotFoundException("Webhook olayına ait sipariş bulunamadı.");
    }

    // olay, siparişin kayıtlı ödeme oturumuna ait olmalıdır (olayın başka bir siparişe yazılmasını engeller)
    if (sessionId != null && !string.IsNullOrEmpty(order.CheckoutSessionId)
                          && !string.Equals(order.CheckoutSessionId, sessionId, StringComparison.Ordinal))
    {
        _logger.LogError(
            "Webhook olayının oturumu siparişte kayıtlı oturumla eşleşmiyor. EventId:{EventId}, OrderId:{OrderId}, OrderSessionId:{OrderSessionId}, EventSessionId:{EventSessionId}",
            stripeEvent.EventId, order.Id, order.CheckoutSessionId, sessionId);

        throw new ConflictException("Webhook olayı siparişin ödeme oturumuyla eşleşmiyor.");
    }

    return order.Id;
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `Guid.TryParse(stripeEvent.OrderId, out ...)` | Metadata'dan gelen metni GUID'e çevirir; **çevirilemezse hata fırlatmaz**, `null` verir | Gövdeden gelen veri bozuk olabilir. Ucuz kontrol: parse edilemiyorsa ikinci yola (oturum id) düşeriz, akış çökmez |
| `IsNullOrWhiteSpace(...) ? null : ...` | Boş/anlamsız oturum id'sini `null` yapar | Sonraki karşılaştırmalarda "boş string" ile "yok" ayrımını ortadan kaldırır; yanlış eşleşmeyi önler |
| `.Where(x => (metadata var mı? && x.Id == ...) \|\| (sessionId var mı? && x.CheckoutSessionId == sessionId))` | Siparişi **iki yoldan biriyle** arar | Metadata kaybolsa bile eşleşme devam eder. Tek yol bırakmak kırılgan olurdu |
| `.AsNoTracking()` | EF Core'un değişiklik izlemeyi kapatır | Sorgu **sadece okuma** amaçlı; güncelleme ayrı SQL ile yapılıyor. Gereksiz izleme maliyeti önlenir |
| `.Select(x => new { x.Id, x.CheckoutSessionId })` | Yalnızca 2 kolon çeker | Tüm sipariş satırını (ve kalemlerini) yüklemek gereksiz veri transferidir |
| `if (order is null)` → `LogError` + `NotFoundException` | Sipariş yoksa **404** ve hata seviyesinde log | En tehlikeli senaryo: para alınmış ama kaydı yok. Sessiz 200 bunu saklar; 404 dönünce Stripe tekrar dener |
| Oturum uyuşmazlığı → `LogError` + `ConflictException` | Olayın oturumu siparişte kayıtlıdan farklıysa **409** | Bir siparişe **başkasının** ödeme sonucunun yazılmasını engeller |
| `!string.IsNullOrEmpty(order.CheckoutSessionId)` koşulu | Karşılaştırma yalnızca siparişte oturum **kayıtlıysa** yapılır | Nakit siparişlerde oturum yoktur; "eşleşmiyor" demek yanlış olurdu. Ayrıca nakit sipariş zaten `Pending` olmadığından güncellenmez |
| `StringComparison.Ordinal` | Oturum id'lerini birebir karşılaştırır | `cs_...` teknik bir kimliktir; harf duyarsızlığı burada **yanlış** olurdu (kültüre bağlı karşılaştırmadan da kaçınır) |
| `return order.Id` | Bulunan siparişin id'si | Sonraki adım (güncelleme) yalnızca bu id ile çalışır |

## 4.4 `ApplyTransitionAsync` — tek SQL'lik güncelleme

```csharp
private async Task<int> ApplyTransitionAsync(Guid orderId, OrderTransition transition,
    CancellationToken cancellationToken)
{
    var query = _orderRepository.GetAllAsQueryable()
        .Where(x => x.Id == orderId && x.PaymentStatus == PaymentStatus.Pending);

    return transition.OrderStatus.HasValue
        ? await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.PaymentStatus, transition.PaymentStatus)
                .SetProperty(x => x.OrderStatus, transition.OrderStatus.Value),
            cancellationToken)
        : await query.ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.PaymentStatus, transition.PaymentStatus),
            cancellationToken);
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `Where(x => x.Id == orderId && x.PaymentStatus == PaymentStatus.Pending)` | **Yalnızca** ödeme sonucu bekleyen siparişi hedefler | **Idempotency'nin kalbi.** İkinci gelen olay hiçbir satırı etkilemez; gecikmiş bir `expired` olayı `Paid` siparişi `Failed`'a düşüremez |
| `ExecuteUpdateAsync(...)` | Kaydı çekmeden **tek SQL `UPDATE`** üretir | Read-modify-write'a göre 0 okuma, daha az yarış penceresi, daha az bellek |
| `transition.OrderStatus.HasValue ? ... : ...` | Sipariş durumu da değişecekse **iki kolonu tek ifadede** yazar | Başarılı ödemede sipariş durumu değişmez; gereksiz kolon yazımı olmaz. İki ayrı `UPDATE` yerine tek ifade → arada tutarsız durum yok |
| `SetProperty(x => x.PaymentStatus, ...)` | Yazılacak ödeme durumu | `Paid` veya `Failed` |
| `SetProperty(x => x.OrderStatus, ...)` | Yalnızca başarısızlık durumunda çalışır | `Canceled` yazılır (domain'deki `MarkExpired()` ile aynı sonuç) |
| Dönüş `Task<int>` | Etkilenen satır sayısı | `Handle` bunu 0 ile karşılaştırır: "gerçekten yazdım mı, yoksa zaten sonuçlanmış mıydı?" |
| `cancellationToken` | İptal desteği | Stripe bağlantıyı kapatırsa yazma işi durur |

> **Alternatif:** `order.MarkPaid()` / `MarkExpired()` domain metotlarını kullanmak daha "domain dostu" olurdu ama iki sorgu gerekir ve arada başka bir isteğin yazması mümkün olurdu. Para akışında **tek, koşullu SQL** tercih edildi; yazılan durum değerleri domain metotlarıyla **aynı** tutuldu.

---

## 4.5 `OrderTransition` — 3 satırlık taşıyıcı tip

```csharp
// Olayın siparişe yansıması: yalnızca ödeme durumu ve (gerekiyorsa) sipariş durumu değişir.
private readonly record struct OrderTransition(PaymentStatus PaymentStatus, OrderStatus? OrderStatus)
{
    public bool Succeeded => PaymentStatus == PaymentStatus.Paid;
}
```

| Kod | Ne yapar | Neden böyle |
| --- | --- | --- |
| `record struct` | Hafif (bellekte kopyalanan) değer tipi | `ResolveTransition` bunu sık üretir; sınıf tahsisi (GC) gerekmez |
| `readonly` | Alanlar sonradan değiştirilemez | Karar verildikten sonra bozulmamalı |
| `PaymentStatus PaymentStatus` | Uygulanacak ödeme durumu | `Paid` veya `Failed` |
| `OrderStatus? OrderStatus` | **İsteğe bağlı** sipariş durumu (`null` = "dokunma") | Başarılı ödemede sipariş durumu değişmemeli; nullable alan bu niyeti taşır |
| `Succeeded => ...` | Mesaj seçiminde kullanılan türetilmiş özellik | `Handle` içinde aynı karşılaştırmayı tekrar yazmamak için |
| `private` | Handler dışına sızmaz | Yalnızca iç karar taşıyıcısı; dışarıya model olarak dönmez |

---

## 4.6 Neden bu kodlar ayrı metotlarda?

| Metot | Ayrı olmasının sebebi |
| --- | --- |
| `ResolveTransition` | "Hangi olay hangi sonuca" kuralını tek yerde toplar; yeni olay eklenince **tek** yere bakılır |
| `IsSessionPaid` | "Para alındı mı?" sorusu beyaz liste mantığıyla tek yerde; güvenlik kararı izole edilmiş |
| `ResolveOrderIdAsync` | Eşleme + iki hata senaryosu (404/409) içerir; `Handle` okunabilir kalsın diye dışarıda |
| `ApplyTransitionAsync` | SQL detayı (EF Core) iş mantığından ayrılır; veri erişimi değişse yalnızca bu metot değişir |
| `OrderTransition` | Kararın taşıyıcısı; "ne yazılacak" + "mesaj ne olacak" bilgisini sadeleştirir |

[→ 05 — İmza doğrulama](05-imza-dogrulama.md)