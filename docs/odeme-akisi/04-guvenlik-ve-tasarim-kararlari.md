# 04 — Güvenlik ve Tasarım Kararları

Bu dosya "neden böyle ve neden böyle **değil**" sorusunun cevabı. Ödeme kodunda verilen her kararın arkasında bir risk vardır; aşağıda risk → önlem → kod eşlemesi yapılmıştır.

---

## 1. Tehdit modeli: neye karşı korunuyoruz?

| Tehdit (saldırı/hata) | Gerçekleşirse ne olur? | Önlem | Kodda nerede |
| --- | --- | --- | --- |
| **Sahte webhook** (adresi bilen biri "ödeme başarılı" atar) | Bedava sipariş | `Stripe-Signature` **HMAC** doğrulaması → 400 | `PaymentService.ParseWebhookEvent` |
| **Gövde değiştirme** (tampering) | Başka sipariş/tutar yazılır | İmza, gövdenin tamamını kapsar; gövde değişirse mühür tutmaz → 400 | Aynı yer |
| **Replay** (eski imzalı isteği kopyalayıp tekrar gönderme) | Aynı ödeme iki kez işlenir | İmzadaki **zaman damgası** penceresi (300 sn) + idempotency | SDK doğrulaması + `Pending` koşulu |
| **Mükerrer teslim** (Stripe aynı olayı tekrar gönderir) | Çift güncelleme, log kirliliği | `WHERE PaymentStatus == Pending` → ikinci kez satır etkilenmez | `ApplyTransitionAsync` |
| **Gecikmiş/karışık sıralı olay** (önce `paid`, sonra `expired` gelir) | Ödenmiş sipariş "başarısız"a düşer | Aynı `Pending` koşulu: sonuçlanmış kayıt **asıla düşürülemez** | Aynı yer |
| **Yanlış siparişe yazma** | A kişisinin parası B siparişine işlenir | Olay oturumu ile siparişin kayıtlı oturumu karşılaştırılır → 409 | `ResolveOrderIdAsync` |
| **Olay kaybı** (sipariş bulunamıyor) | Ödeme alındı ama sistemde iz yok | Sessiz 200 yerine **404** → Stripe tekrar dener + `LogError` | Aynı yer |
| **DoS** (dev gövde / şişirilmiş başlık) | CPU/bellek tüketimi | Validator: imza ≤ 1024, gövde ≤ 1 MB | `ProcessStripeWebhookCommandValidator` |
| **Sır sızıntısı** | Saldırgan geçerli imza üretebilir | Secret yalnızca user-secrets/env'de; loglara **asla** yazılmaz | `StripeSettings`, log politikası |
| **Kişisel veri sızıntısı** (log) | KVKK/GDPR riski | Gövde loglanmaz; sadece kimlikler | Handler/Service logları |
| **IDOR** (başkasının sepetini siparişe çevirme) | Yetkisiz sipariş | `CurrentUser` akışında `UserId` **token'dan** okunur, gövdeden asla | `CheckoutController`, `[BindNever]` |
| **Yetkisiz panel kullanımı** | Personel olmayan biri masa siparişi açar | `checkout:create` policy'si | `[Authorize(Policy = ...)]` |
| **Bilgi sızdırma** (pasif masa/enumerasyon) | Rakip masa durumunu öğrenir | Pasif masa da "Masa bulunamadı." (404) | `CartService.ValidateTargetForCheckout` |

---

## 2. Neden imza doğrulaması "tek başına yeterli" sayılıyor?

Doğrulanmış bir webhook gövdesine **güvenebiliriz**, çünkü:

1. Gövdeyi yalnızca Stripe (ve biz) bilinen gizli anahtarla imzalayabilir.
2. İmza gövdenin **her baytını** kapsar; araya bir alan eklenirse doğrulama başarısız olur.

Ama yine de **gövdedeki her şeye körü körüne güvenmiyoruz**:

| Gövdedeki bilgi | Kullanıyor muyuz? |
| --- | --- |
| `order_id` (bizim yazdığımız metadata) | ✅ Sadece siparişi **bulmak** için |
| `session.id` | ✅ Bulmak + uyuşmazlık kontrolü için |
| `payment_status` | ✅ Kararı etkiler, ama yalnızca izin verilen değerlerde (`paid`, `no_payment_required`) |
| Tutar (`amount_total`) | ❌ **Hiç kullanılmaz.** Tutar bizim siparişimizde saklıdır; Stripe'ın söylediği tutarla güncelleme yapmayız |
| Müşteri adı/adresi/e-postası | ❌ Kullanılmaz (adres snapshot'ı sipariş oluşturulurken yazılır) |
| Ödeme yöntemi detayları | ❌ Kullanılmaz |

Bu ayrım önemli: **"imzalı" demek "her alanı iş kuralına sok" demek değildir.** Yalnızca karar için gereken minimum alanlar okunur. Böylece Stripe tarafındaki bir alan değişse bile sistemimiz bozulmaz.

---

## 3. Neden Stripe kodu Infrastructure'da, imza doğrulaması Application'da değil?

```
Domain  ←  Application  ←  Infrastructure  ←  WebApi
(kural)     (iş akışı)      (dış dünya)        (HTTP)
```

| Karar | Neden |
| --- | --- |
| Stripe kütüphanesi yalnızca `Infrastructure`'da | Dış servis bağımlılığı içeri sızmaz. Application, Stripe'ın tiplerini görmediği için sağlayıcı değişse iş akışı aynı kalır |
| Application'da yalnızca **arayüz** var (`IPaymentService`) | Handler "ödeme servisi" soyutlamasına bağlıdır; testlerde sahtelenebilir |
| Stripe'ın string olay isimleri → **enum** | İş mantığı sağlayıcının yazım kurallarına bağımlı olmaz; derleyici kontrol eder |
| İhtiyaç duyduğumuz 5 alan ayrı modelde (`StripeWebhookEvent`) | Stripe'ın tüm olay modelini içeri taşımak gereksiz kırılganlık yaratır |

---

## 4. HTTP kodları neden böyle seçildi?

| Durum | Kod | Neden bu kod? |
| --- | --- | --- |
| Alan hataları (boş imza/gövde) | **400** | İstek biçimsel olarak geçersiz |
| İmza doğrulanamadı | **400** | "İsteği yapan sen değilsin" — istemci hatası. 401 kullanmıyoruz çünkü ortada bir kimlik doğrulama şeması yok, 403 kullanmıyoruz çünkü yetki değil **gerçeklik** problemi |
| Sipariş/oturum yok | **404** | Hedef kaynak yok; Stripe tekrar dener (istediğimiz davranış) + log |
| Oturum uyuşmazlığı | **409** | Mevcut kayıtla **çakışan** bir istek; insan müdahalesi gerektiren durum |
| Webhook secret eksik | **500** | Sunucu yapılandırma hatası (istemcinin düzeltebileceği bir şey değil) |
| Bilinmeyen/ilgisiz olay | **200** | Stripe'ın gereksiz retry'ını engeller |
| Mükerrer olay | **200** | İstediğimiz sonuç zaten sağlanmış (idempotent başarı) |

**Altın kural:** 2xx = "tekrar gönderme", 4xx/5xx = "tekrar gönder".
Bu yüzden işlenmeyen olayda 200, sipariş bulunamadığında 404 dönüyoruz.

## 5. Değerlendirilen alternatifler ve neden seçilmedi

| Alternatif yaklaşım | Neden seçilmedi |
| --- | --- |
| **Webhook yerine müşterinin dönüşünde** (`SuccessUrl` sayfası) durumu güncellemek | Müşteri sekmeyi kapatabilir; o zaman para alınmış olur ama sipariş `Pending` kalır. Ayrıca tarayıcıdan gelen istek kanıt değildir |
| **Kredi kartı bilgisini bizim formdan almak** | PCI-DSS kapsamına girer; kart verisi saklamak/dokunmak yasal ve operasyonel olarak ağırdır. Stripe'ın kendi sayfasında bırakılır |
| **Siparişi önce veritabanına yazıp sonra Stripe oturumu açmak** | DB yazımı başarılı olup Stripe hata verirse "ödeme linki olmayan sipariş" oluşur (kullanıcıya dönük kırık kayıt). Şimdiki sıralamada en kötü durum "kullanılmayan link"dir |
| **Webhook'ta kaydı okuyup `MarkPaid()` çağırıp kaydetmek** | İki sorgu + "oku-değiştir-yaz" yarış penceresi. Koşullu tek SQL (`ExecuteUpdateAsync`) para akışında daha güvenli |
| **Webhook uçuna JWT/temel kimlik doğrulama eklemek** | Stripe token/style göndermez; WAF/rate limit ile birlikte imza doğrulaması standart ve yeterli yaklaşımdır |
| **`completed` olayında doğrudan `Paid` yazmak** | Asenkron ödeme yöntemlerinde ödenmemiş sipariş `Paid` görünür; **para kaybı** riski |
| **İşlenmeyen olayda 400 dönmek** | Stripe günlerce tekrar dener; log ve trafik kirlenir |
| **Sipariş bulunamadığında 200 dönmek** | Gerçek bir ödeme kaydı sessizce kaybolur (en tehlikeli seçenek) |
| **Tutarı webhook gövdesinden yazmak** | İş kuralı dışarıya devredilmiş olur; sipariş tutarı ile tahsilat arasında tutarsızlık riski |
| **Ayrı bir `IStripeWebhookService` arayüzü** | Ödeme sağlayıcısı bilgisi iki ayrı soyutlamaya bölünürdü. `IPaymentService` "ödeme sağlayıcısı" soyutlaması olarak tek noktada toplandı |

---

## 6. Bilinçli olarak kabul edilen riskler (ve sertleştirme önerileri)

| Kalan risk | Etkisi | Önerilen ek adım |
| --- | --- | --- |
| Rate limiting yok | Saldırgan uca çok istek atıp CPU harcatabilir (imza doğrulaması maliyetli) | Webhook rotasına özel rate limit + gövde limiti (Kestrel) |
| Stripe IP allowlist yok | İmza zaten koruyor, ek katman yok | Stripe'ın yayınladığı IP blokları ile allowlist |
| İşlenen olayların kaydı tutulmuyor | Aynı `event.id` teorik olarak tekrar gelirse yine sorunsuz (idempotent), ama denetim izi yok | `ProcessedStripeEvents` tablosu + `event.id` unique index |
| Webhook için ayrı alarm yok | İmza hataları/404'ler gözden kaçabilir | Log tabanlı uyarı (ör. "Webhook imzası doğrulanamadı" artarsa alarm) |
| Ödeme sonrası mutfak durumu | Sipariş `Paid` olur ama `OrderStatus` ayrı akış | Mutfak durum uçları/otomasyonu (bu dokümanın kapsamı dışı) |

Bu maddeler "eksik" değil, **bilinçli ertelenmiş** işlerdir: mevcut tasarımın doğruluğunu bozmazlar, yalnızca dayanıklılık/gözlemlenebilirlik ekler.

---

## 7. Bu kararları ne zaman gözden geçirmeliyiz?

| Tetikleyici | Gözden geçirilecek karar |
| --- | --- |
| İade/iptal akışı eklenirse | `PaymentStatus.Refunded` ve `charge.refunded` benzeri olaylar |
| Aynı sipariş için birden çok ödeme denemesi istenirse | Sipariş ↔ oturum ilişkisi (şu an 1 sipariş = 1 oturum, alan siparişte tek) |
| Ödeme sağlayıcısı değişirse/eklenirse | `IPaymentService` arkasında yeni implementasyon; iş akışı değişmez |
| Farklı para birimi | `ToMinorUnits` ve `Currency` ayarı; kuruş varsayımı sıfır/iki haneli birimlere göre gözden geçirilmeli |
| Webhook trafiği artarsa | `event.id` tablosu + arka planda işleme (kuyruk) |

---

## 8. Hızlı kontrol listesi (kod incelemesi için)

- [ ] Her yeni webhook olayı için "hangi duruma çeviriyoruz?" sorusu `ResolveTransition`'a işlendi mi?
- [ ] Yeni olay **para durumunu kesinleştiriyor mu**, yoksa "belki" mi diyor? (Belirsizse yoksay)
- [ ] Güncelleme hâlâ `Pending` koşuluyla mı yapılıyor? (Idempotency bozulmasın)
- [ ] Yeni bir alan okunuyorsa: imzalı olsa bile **iş kuralı için gerekli mi**?
- [ ] Log'a payload/sır/kişisel veri sızmıyor mu?
- [ ] Sağlayıcıya özel tip Application katmanına sızdı mı? (Kaçak varsa `IPaymentService`'e taşınmalı)

Devamı: [05 — Senaryolar ve örnekler](05-senaryolar-ve-ornekler.md)