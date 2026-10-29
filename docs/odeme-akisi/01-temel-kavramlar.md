# 01 — Temel Kavramlar (sıfırdan, benzetmelerle)

Bu dosya ödeme işine yeni giren biri için yazıldı. Kod detayı yok; **kavramlar** var.
Aşağıdaki kavramları anlarsan, [02](02-siparis-olusturma-akisi.md) ve [03](03-webhook-odeme-sonucu-akisi.md) dosyalarındaki her satır kendiliğinden anlamlı olur.

---

## 1. Neden Stripe var?

Kredi kartı almak **kendi başımıza yapabileceğimiz** bir iş değildir:

- Kart bilgisini saklamak/dokunmak **yasak ve tehlikeli** (PCI-DSS denen bir standart var; uymayan firmaya ceza kesilir).
- 3D Secure, banka onayı, taksit, iade, itiraz (chargeback) gibi işlerin tamamı bankalar arası bir dünyadır.
- Biz restoran yönetim yazılımı yazıyoruz; bu işi bizim yerimize **ödeme sağlayıcısı** yapar.

**Benzetme:** Stripe = marketteki **kasa**. Biz kasayı işletmiyoruz; müşteriyi kasa sırasına sokuyoruz ve kasadan gelen fişi (sonucu) işliyoruz.

> Bizim sunucumuz **kart numarasını asla görmez**. Müşteri kart bilgisini Stripe'ın kendi sayfasında girer. Bu hem güvenlik hem de yasal zorunluluktur. Projede bu yüzden `PaymentService` yalnızca *oturum açar* ve *sonucu okur*; kart verisi modellerimizde hiç yoktur.

---

## 2. Checkout Session (ödeme oturumu) nedir?

Stripe'a dediğimiz şey: **"Şu ürünleri, şu tutarla, şu kişi için ödeme sayfası oluştur."**
Stripe bize üç şey döner:

| Dönen | Anlamı |
| --- | --- |
| `SessionId` (`cs_...`) | Oturumun **kimlik numarası**; sonradan "şu ödeme ne oldu?" diye sorarken kullanılır |
| `SessionUrl` | Müşterinin gideceği **ödeme sayfasının adresi** |
| `ExpiresAt` | Oturumun **son kullanma zamanı** (biz 1 saat koyduk) |

**Benzetme:** Oturum = kasadaki **fiş numarası**, `SessionUrl` = müşteriyi ödeme ekranına götüren yol.

Neden oturumu biz kurmuyoruz da Stripe kuruyor? Çünkü sayfa Stripe'ın alan adında çalışır (`checkout.stripe.com`), kart bilgisi orada işlenir, **bize hiç uğramaz**.

---

## 3. Bir oturumun iki ayrı "durum"u vardır (çok önemli!)

Bu ayrımı karıştırmak gerçek projelerde **en sık yapılan ödeme hatasıdır**:

| Alan | Sorduğu soru | Örnek değerler |
| --- | --- | --- |
| Oturumun **kendi durumu** (`status`) | "Ödeme **sayfası** ne durumda? Açık mı, kapandı mı?" | `open`, `complete`, `expired` |
| Oturumun **ödeme durumu** (`payment_status`) | "**Para** gerçekten alındı mı?" | `paid`, `unpaid`, `no_payment_required` |

Kritik gerçek: **Sayfa "complete" olabilir ama para henüz alınmamış olabilir.**
Bazı ödeme yöntemleri **asenkron** çalışır: müşteri işlemi başlatır, para dakikalar/günler sonra onaylanır.

**Benzetme:** Kasadaki görevli fişi kapattı (`complete`) ama para hesaba **yarın** geçecek.

Bu yüzden kodumuz `checkout.session.completed` olayını tek başına "para geldi" saymaz; `payment_status` alanına **bakar** (bkz. [03](03-webhook-odeme-sonucu-akisi.md)).

---

## 4. Webhook nedir, neden şart?

**Webhook = "olay olduğunda bizi arayan" ters yönlü istek.**
Normalde biz Stripe'a soru sorarız; webhook'ta **Stripe bize** "şu oturumun ödemesi tamamlandı" diye POST atar.

**Benzetme:** Webhook = restoranın **kapı zili**. Kurye gelince zil çalar, biz kapıya bakarız. Her 5 dakikada bir "kurye geldi mi?" diye kapıya koşmayız (buna *polling* denir; hem gereksiz yük hem gecikme).

Neden şart:

1. **Müşteri bize dönmeyebilir.** Ödeme sonrası sekmeyi kapatırsa bizim "başarılı" sayfamız hiç açılmaz ama para alınmış olabilir.
2. **Asenkron sonuçlar** (yukarıdaki `payment_status` konusu) yalnızca olayla öğrenilir.
3. **Tek doğruluk kaynağı Stripe'tır.** Müşterinin tarayıcısından gelen "ödedim" bilgisi kanıt değildir; saldırgan da aynı isteği atabilir.

**Bizim durumumuzda:** Sipariş doğar ama `PaymentStatus = Pending` kalır. Webhook gelince `Paid` ya da `Failed` olur. Bu bilgi **tek bir yerde** (webhook) yazılır; iki yerde yazılsa birbiriyle çakışır.

## 5. İmza (signature) ve HMAC nedir?

Webhook ucunun adresini bilen herkes oraya istek atabilir. "Postacı kimliği" olmadan bu ucu açmak, herkesin "ödeme başarılı" diye sahte istek atıp bedava sipariş kapatması demektir.

Stripe bunu **imza** ile çözer:

1. Stripe panelinde bir **gizli anahtar** (webhook secret, `whsec_...`) üretiriz. Bu anahtar **sadece Stripe'ta ve bizim sunucumuzda** bulunur.
2. Stripe istek atarken gövdeyi bu anahtarla matematiksel olarak karıştırıp bir özet üretir ve `Stripe-Signature` başlığına koyar.
3. Biz **aynı hesabı kendi anahtarımızla tekrar yaparız**. İki sonuç aynıysa istek gerçekten Stripe'tan gelmiştir.

Bu yöntemin adı **HMAC-SHA256**'dır. Tek yönlüdür: imzadan anahtarı geri bulmak mümkün değildir.

**Benzetme:** İmza = **mühürlü zarf**. Zarf açılıp mektup değiştirilmişse ya da sahte zarf gönderilmişse mühür tutmaz.

İki ek koruma:

- **Zaman damgası (`t=...`):** İmza "şu an" için üretilmiştir. Eski bir imza kopyalanıp tekrar gönderilirse (buna **replay attack** denir) tolerans penceresi (Stripe varsayılanı **300 saniye**) dışında kaldığı için reddedilir.
- **Anahtar sadece sunucuda:** Anahtar sızarsa herkes imza üretebilir; bu yüzden kodda değil, **user-secrets / environment** içinde tutulur (`Stripe:WebhookSecret`).

---

## 6. Idempotency (aynı işlemi iki kez yapmama) nedir?

İnternette hiçbir şey "tam bir kez" olmaz: Stripe aynı olayı **tekrar** gönderebilir (biz 200 dönmedik diye, ağ koptu diye, kendi içinde tekrar denedi diye). Aynı isteği iki kez aldığımızda **iki kez işlem yapmamalıyız**.

Projede iki yerde idempotency var:

| Yer | Mekanizma | Neyi engeller |
| --- | --- | --- |
| Sipariş açılırken | `X-Idempotency-Key` başlığı (Stripe'a da iletilir) | Müşteri "öde" butonuna iki kez basarsa **iki sipariş** açılmasını |
| Webhook işlenirken | Yalnızca `PaymentStatus == Pending` olan sipariş güncellenir | Aynı olayın ikinci kez yazılmasını ve **sonuçlanmış ödemenin geri alınmasını** |

**Benzetme:** Aynı fişi kasaya iki kez uzatsan da para bir kez alınır; ikinci uzatışta "bu işlem yapılmış" denir.

---

## 7. Tutar birimleri: TRY vs kuruş

- **Veritabanında tutarlar TRY** olarak tutulur (ör. `125.50`).
- **Stripe en küçük birimi ister** → kuruş (ör. `12550`). Çevrim kodda `ToMinorUnits()` ile yapılır (`× 100`, iki haneye yuvarlama).

Neden? Stripe `UnitAmount` alanını **tam sayı** bekler; parayı `double` ile tutmak yuvarlama hatası üretir (meşhur `0.1 + 0.2` problemi). Projede para her yerde `decimal`.

---

## 8. Metadata: Stripe'ın yanına koyduğumuz notlar

Oturum açarken Stripe'a **kendi etiketlerimizi** gönderiyoruz:

| Anahtar | Değer | Neden gerekli |
| --- | --- | --- |
| `order_id` | Siparişimizin GUID'i | Webhook geldiğinde **hangi sipariş** olduğunu anında bulmak için |
| `checkout_request_type` | `CurrentUser` / `PanelUser` | Loglarda kimin ödediğini ayırt etmek için |
| `user_id` | Kullanıcı id'si (masa akışında boş) | Destek/araştırma kolaylığı |
| `restaurant_table_id` | Masa id'si (kullanıcı akışında boş) | Destek/araştırma kolaylığı |

**Benzetme:** Kargo paketine yazdığın **takip numarası**. Webhook gövdesinde bu numara olduğu için siparişi aramakta zorlanmayız.

---

## 9. Özet tablo

| Kavram | Tek cümlede |
| --- | --- |
| Stripe | Ödeme işini bizim yerimize yapan sağlayıcı (kasa) |
| Checkout Session | "Şu ürünleri şu tutarla ödet" talebiyle açılan ödeme oturumu (fiş) |
| `status` | Ödeme **sayfasının** durumu (`open`/`complete`/`expired`) |
| `payment_status` | **Paranın** alınıp alınmadığı (`paid`/`unpaid`) |
| Webhook | Olay olduğunda Stripe'ın bizi aradığı ters yönlü uç (kapı zili) |
| İmza / HMAC | İsteğin gerçekten Stripe'tan geldiğinin kanıtı (mühürlü zarf) |
| Replay koruması | Eski imzanın tekrar kullanılmasını engelleyen zaman penceresi |
| Idempotency | Aynı iş iki kez gelse de sonucun bir kez oluşması |
| Kuruş | Stripe'ın beklediği en küçük para birimi |
| Metadata | Oturuma iliştirdiğimiz kendi notlarımız (`order_id` vb.) |

Devamı: [02 — Sipariş oluşturma akışı](02-siparis-olusturma-akisi.md)
