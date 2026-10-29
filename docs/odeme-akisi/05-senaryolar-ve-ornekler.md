# 05 — Senaryolar ve Örnekler (gerçek JSON'larla)

Bu dosyada ödeme akışının **uçtan uca senaryoları** var: istek → kodun yaptığı iş → veritabanı sonucu → cevap.
JSON örnekleri Stripe olaylarının gerçek şekline uygundur (gövde kısaltılmıştır, ilgili alanlar bırakılmıştır).

---

## Senaryo 1 — Mutlu yol (senkron kart ödemesi)

### 1.1 Sipariş açılır

```http
POST /api/checkout/users
Authorization: Bearer <access_token>
X-Idempotency-Key: 8f14e45f-ea0c-4b1a-9f4a-2b4c1d9a7f33
```

**Ne olur:** sepet doğrulanır → sipariş `OrderStatus=Pending`, `PaymentStatus=Pending` oluşur → Stripe'da 1 saatlik oturum açılır → `sessionId`/`sessionUrl` siparişe yazılır → sepet kalemleri silinir.

```json
{
  "sessionUrl": "https://checkout.stripe.com/c/pay/cs_test_a1b2c3",
  "code": "CHECKOUT_SESSION_CREATED"
}
```

### 1.2 Müşteri öder, Stripe webhook atar

```http
POST /api/webhooks/checkout
Stripe-Signature: t=1793000000,v1=5f3c9d...   ← SDK tarafından hesaplanmış HMAC
Content-Type: application/json
```

```json
{
  "id": "evt_1Q2w3e4r5t6y",
  "object": "event",
  "type": "checkout.session.completed",
  "data": {
    "object": {
      "id": "cs_test_a1b2c3",
      "object": "checkout.session",
      "payment_status": "paid",
      "amount_total": 45000,
      "metadata": {
        "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
        "checkout_request_type": "CurrentUser",
        "user_id": "9b2f1c44-1f0e-4a3c-9b0e-6d0f5c9a1b27",
        "restaurant_table_id": ""
      }
    }
  }
}
```

**Kodun yaptığı iş, sırayla:**

| Adım | İşlem | Sonuç |
| --- | --- | --- |
| 1 | `Stripe-Signature` + ham gövde imza doğrulaması | ✅ Geçerli |
| 2 | `type` → `CheckoutSessionCompleted` | Eşleşti |
| 3 | `payment_status = "paid"` | "Para gerçekten alındı" → `Paid` kararı |
| 4 | `metadata.order_id` çözümlenir | Sipariş id'si bulundu |
| 5 | `UPDATE Orders SET PaymentStatus = Paid WHERE Id = ... AND PaymentStatus = Pending` | 1 satır etkilendi |
| 6 | Cevap | `200` + `STRIPE_WEBHOOK_PROCESSED` |

**Veritabanı sonucu:** `PaymentStatus = Paid`, `OrderStatus = Pending` (mutfak akışı ayrı iştir).

```json
{ "message": "Ödeme onaylandı, sipariş ödendi olarak işaretlendi.", "code": "STRIPE_WEBHOOK_PROCESSED" }
```

---

## Senaryo 2 — Asenkron ödeme (para sonra geliyor)

Bazı ödeme yöntemlerinde önce "oturum tamamlandı ama para yok" olayı gelir.

### 2.1 İlk olay: sayfa bitti, para henüz gelmedi

```json
{
  "id": "evt_async_step1",
  "type": "checkout.session.completed",
  "data": { "object": { "id": "cs_test_async1", "payment_status": "unpaid",
    "metadata": { "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301" } } }
}
```

**Sonuç:** `ResolveTransition` → `null` → **hiçbir şey yazılmaz**, `200` + `STRIPE_WEBHOOK_IGNORED` + log.
**Neden önemli:** Burada `Paid` yazsaydık, henüz alınmamış bir para için sipariş üretime giderdi.

### 2.2 İkinci olay: para geldi

```json
{
  "id": "evt_async_step2",
  "type": "checkout.session.async_payment_succeeded",
  "data": { "object": { "id": "cs_test_async1", "payment_status": "paid",
    "metadata": { "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301" } } }
}
```

**Sonuç:** `PaymentStatus = Paid` → `200` + `STRIPE_WEBHOOK_PROCESSED`.

### 2.3 Alternatif son: para gelmedi

```json
{
  "id": "evt_async_fail",
  "type": "checkout.session.async_payment_failed",
  "data": { "object": { "id": "cs_test_async1", "payment_status": "unpaid",
    "metadata": { "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301" } } }
}
```

**Veritabanı sonucu:** `PaymentStatus = Failed` + `OrderStatus = Canceled`; cevap `200` + mesaj:
`"Ödeme başarısız olduğu için sipariş iptal edildi."`

---

## Senaryo 3 — Ödeme sayfası açıldı ama müşteri ödemedi (süre doldu)

```json
{
  "id": "evt_expired_1",
  "type": "checkout.session.expired",
  "data": { "object": { "id": "cs_test_a1b2c3", "payment_status": "unpaid",
    "metadata": { "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301" } } }
}
```

**Sonuç:** `PaymentStatus = Failed` + `OrderStatus = Canceled`; `200` + `STRIPE_WEBHOOK_PROCESSED`.
**Neden otomatik iptal?** Ödeme alınmadı; sipariş `Pending` kalırsa raporlar ve mutfak ekranı yanıltıcı olur. (Beklemede kalmasını isterseniz: [04](04-guvenlik-ve-tasarim-kararlari.md) §3'teki tek satırlık değişiklik.)

---

## Senaryo 4 — Mükerrer teslim (Stripe aynı olayı iki kez gönderir)

Aynı `evt_1Q2w3e4r5t6y` gövdesi **ikinci kez** gelir:

| Adım | İşlem | Sonuç |
| --- | --- | --- |
| 1 | İmza doğrulaması | ✅ Geçerli |
| 2 | Karar | `Paid` (yine) |
| 3 | `UPDATE ... WHERE PaymentStatus = Pending` | **0 satır** (sipariş artık `Pending` değil) |
| 4 | Cevap | `200` + `"Olay daha önce işlenmiş, sipariş durumu değiştirilmedi."` |

**Kritik nokta:** İkinci istek **hiçbir şeyi bozmaz**. `WHERE` koşulu olmasaydı, gecikmiş bir `expired` olayı `Paid` siparişi `Failed`'a düşürebilirdi — bu, "para alındı ama sipariş iptal" gibi en kötü senaryodur.

## Senaryo 5 — Sahte istek ("ben Stripe'ım" diyen saldırgan)

```http
POST /api/webhooks/checkout
Stripe-Signature: t=1793000000,v1=deadbeefdeadbeef
Content-Type: application/json
```

```json
{ "id": "evt_fake", "type": "checkout.session.completed",
  "data": { "object": { "id": "cs_sahte", "payment_status": "paid",
    "metadata": { "order_id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301" } } } }
```

| Olası varyasyon | Sonuç |
| --- | --- |
| İmza başlığı hiç yok | `400` (validator: "Webhook imzası (Stripe-Signature) zorunludur.") |
| İmza var ama yanlış | `400` + "Webhook imzası doğrulanamadı." + `LogWarning` |
| Gerçek imza kopyalanmış ama **1 saat geçmiş** | `400` (zaman toleransı dışı — replay) |
| İmza doğru, ama gövde arada değiştirilmiş | `400` (imza tutmaz) |
| Gövde 1 MB'tan büyük | `400` (validator boyut sınırı) |

**Her durumda:** veritabanına **hiçbir yazma** olmaz. Kod, şüpheli durumda "hiçbir şey yapmamak" tarafındadır.

---

## Senaryo 6 — Olay geldi ama sipariş yok

```json
{ "id": "evt_orphan", "type": "checkout.session.completed",
  "data": { "object": { "id": "cs_baska_ortam", "payment_status": "paid",
    "metadata": { "order_id": "11111111-1111-1111-1111-111111111111" } } } }
```

**Sonuç:** `404` + "Webhook olayına ait sipariş bulunamadı." + `LogError`.

**Neden 200 değil?** Çünkü ödeme **alınmış ama kaydı yok** olabilir; bu durumda sessiz kalmak parayı kaybetmek demektir. 404 dönünce Stripe olayı tekrar göndermeye devam eder ve operatör panelde/logda görür.

**Tipik sebepler:** başka ortamın (test/prod) olayı yanlış uca yönlendirilmiş, sipariş kaydı elle silinmiş, metadata üretilirken hata olmuş.

---

## Senaryo 7 — Nakit (cash) masa siparişi — webhook hiç devreye girmez

```http
POST /api/checkout/tables/{tableId}/cash
Authorization: Bearer <panel_token>          ← checkout:create izni
X-Idempotency-Key: b1d3f2a4-...
```

**Sonuç:** sipariş **tek `INSERT`** ile `OrderStatus = Confirmed` + `PaymentStatus = Paid` olarak yazılır; `CheckoutSessionId` boş kalır.

```json
{ "message": "Masa siparişi oluşturuldu, ödeme nakit olarak alınacaktır.", "code": "CHECKOUT_SUCCESS" }
```

**Neden webhook gerekmez?** Ödeme masada elden alınır; sonuç baştan kesindir, bekleyecek bir "ödeme sonucu" yoktur. Bu yüzden bu akışta ne Stripe oturumu ne de webhook vardır.

---

## Senaryo 8 — Karışık masa (panel) online ödeme

Panel personeli masanın siparişini açıp müşterinin (ya da kendisinin) ödemesi için link üretir:

| Fark | Kullanıcı akışı | Masa akışı |
| --- | --- | --- |
| Sipariş sahibi | `UserId` dolu | `RestaurantTableId` dolu, `UserId` boş |
| Adres | Varsayılan adres şart | Adres yok |
| Kargo | Kural: 500 TL üstü ücretsiz, altı 120 TL | **0** |
| Webhook eşlemesi | `metadata.order_id` → sipariş | `metadata.order_id` → sipariş (aynı yol) |
| Yetki | Sadece login | `checkout:create` izni |

Webhook tarafında iki akış **hiç ayrışmaz**: olay tipi + `payment_status` yeterlidir, sahibin kullanıcı mı masa mı olduğu sonucu değiştirmez.

---

## Özet tablo: olay → kod → sonuç

| Gelen olay | `payment_status` | Kod kararı | DB sonucu | HTTP |
| --- | --- | --- | --- | --- |
| `checkout.session.completed` | `paid` / `no_payment_required` | `Paid` | `Paid`, `Pending` | 200 |
| `checkout.session.completed` | `unpaid` / diğer | yoksay | değişmez | 200 (IGNORED) |
| `checkout.session.async_payment_succeeded` | — | `Paid` | `Paid`, `Pending` | 200 |
| `checkout.session.async_payment_failed` | — | `Failed` | `Failed`, `Canceled` | 200 |
| `checkout.session.expired` | — | `Failed` | `Failed`, `Canceled` | 200 |
| Aynı olayın tekrarı | — | karar aynı | **değişmez** | 200 |
| Bilinmeyen olay | — | yoksay | değişmez | 200 (IGNORED) |
| İmza geçersiz/boş | — | reddet | değişmez | 400 |
| Sipariş bulunamadı | — | hata | değişmez | 404 |
| Oturum uyuşmazlığı | — | hata | değişmez | 409 |

---

## Yerelde denemek istersen (Stripe CLI)

Gerçek Stripe olaylarını makinene yönlendirmek için (opsiyonel, üretim kodunu etkilemez):

```bash
# Stripe CLI kurulduysa:
stripe login
stripe listen --forward-to http://localhost:5269/api/webhooks/checkout
# CLI bize geçici bir whsec_... verir; bunu kullanıcı sırlarına yaz:
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
# Sonra örnek bir olay tetikle:
stripe trigger checkout.session.completed
```

> `stripe listen` çıktısındaki imza anahtarı **sadece o oturum için**dir; kalıcı kullanmak için Stripe panelinden webhook uç noktası tanımlayıp kendi `whsec_...` değerini kullanmalısın.

Devamı: [06 — Sözlük](06-sozluk.md)