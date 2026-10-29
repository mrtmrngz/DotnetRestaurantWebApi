# 01 — `WebhooksController.cs` (satır satır)

**Dosya:** `src/RestaurantApi.WebApi/Controllers/WebhooksController.cs`
**Görev:** Stripe'ın attığı isteği karşılamak, gövdeyi ham olarak okumak, komutu kurup MediatR'a vermek, cevabı döndürmek. **İş kuralı içermez.**

---

## 1.1 Dosyanın tamamı

```csharp
using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;
using RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    // Stripe imzayı bu başlıkta gönderir.
    private const string StripeSignatureHeader = "Stripe-Signature";

    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Stripe checkout ödeme olaylarını işler.
    /// </summary>
    /// <remarks>
    /// İşlenen olaylar: `checkout.session.completed`, `checkout.session.async_payment_succeeded`,
    /// `checkout.session.async_payment_failed`, `checkout.session.expired`.
    ///
    /// Akış:
    /// 1. İstek gövdesi **ham (raw)** hâliyle okunur; imza doğrulaması gövdenin değiştirilmemiş hâli üzerinden yapılmalıdır.
    /// 2. `Stripe-Signature` başlığı ve ham gövde komuta aktarılır; imza doğrulaması ve olayın siparişe yansıtılması handler içindedir.
    /// 3. Bu uç **anonimdir** (Stripe token göndermez); güvenlik webhook imzasının doğrulanmasıyla sağlanır.
    ///
    /// Bu uç yalnızca Stripe'ın çağırması içindir; Stripe 2xx dışındaki yanıtlarda olayı tekrar gönderir,
    /// bu yüzden işleme adımları idempotent olmalıdır.
    /// </remarks>
    /// <param name="signature">Stripe'ın gönderdiği `Stripe-Signature` başlığı (zaman damgası ve imzayı taşır).</param>
    /// <param name="cancellationToken">İstemci bağlantısı kesildiğinde işlemi durdurmak için kullanılır.</param>
    /// <response code="200">Olay alındı ve işlendi; işlenmeyen olaylarda da 200 döner (Stripe tekrar denemesin).</response>
    /// <response code="400">İmza başlığı eksik/geçersiz, imza doğrulanamadı veya gövde çözümlenemedi.</response>
    /// <response code="404">Olayın ait olduğu sipariş bulunamadığında (kayıp ödeme olmaması için 2xx dönülmez).</response>
    /// <response code="409">Olayın oturumu siparişte kayıtlı ödeme oturumuyla eşleşmediğinde.</response>
    [HttpPost("checkout")]
    #region Swagger Docs
    [ProducesResponseType(typeof(BaseResponse), 200)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(StripeWebhookProcessedResponseExample))]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundErrorExample))]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ConflictErrorExample))]
    #endregion
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> CheckoutWebhookRoute(
        [FromHeader(Name = StripeSignatureHeader), Required] string signature,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);

        string payload = await reader.ReadToEndAsync(cancellationToken);

        var command = new ProcessStripeWebhookCommand
        {
            Signature = signature,
            Payload = payload
        };

        BaseResponse response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}
```

---

## 1.2 Class ve route satırları

```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
```

| Kod | Ne yapar | Neden gerekli |
| --- | --- | --- |
| `[ApiController]` | Otomatik model doğrulama, doğru 400 davranışı, Swagger entegrasyonu sağlar | Projedeki tüm controller'lar bu kalıpta |
| `[Route("api/[controller]")]` | `[controller]` → sınıf adının başındaki kısmı küçük harfe çevirir → **`api/webhooks`** | Route'u elle yazmak yerine sınıf adıyla tutarlı tutar |
| `: ControllerBase` | HTTP yardımcılarını (`Ok`, `Request`, `StatusCodes`) getirir | View (HTML) döndürmediğimiz için `Controller` değil `ControllerBase` |

**Metotla birlikte tam adres:** `api/webhooks` + `[HttpPost("checkout")]` = **`POST api/webhooks/checkout`**

---

## 1.3 Sabitler ve bağımlılıklar

```csharp
private const string StripeSignatureHeader = "Stripe-Signature";

private readonly IMediator _mediator;

public WebhooksController(IMediator mediator)
{
    _mediator = mediator;
}
```

| Satır | Ne yapar | Neden böyle |
| --- | --- | --- |
| `StripeSignatureHeader = "Stripe-Signature"` | İmza başlığının adını tek yerde tutar | Aynı metni iki yere (attribute + doküman) yazsaydık biri değişince diğeri unutulurdu. Yazım hatası riski **sıfıra** iner: Stripe bu başlık adını değiştirirse tek yerden düzeltilir |
| `IMediator` | İsteği handler'a taşıyan araç | Controller iş mantığını **bilmez**; sadece "şu komutu işle" der. Böylece kural değişince controller'a dokunulmaz |
| Constructor injection | Bağımlılıklar dışarıdan verilir | Test edilebilirlik: testte sahte `IMediator` verilebilir |

> `ICurrentUserService` yok: bu uçta oturum açan kullanıcı kavramı yoktur. Kimlik doğrulaması **imza** ile yapılır.

## 1.4 Metodu tanımlayan satırlar

```csharp
[HttpPost("checkout")]
[AllowAnonymous]
[Consumes("application/json")]
public async Task<IActionResult> CheckoutWebhookRoute(
    [FromHeader(Name = StripeSignatureHeader), Required] string signature,
    CancellationToken cancellationToken)
```

| Kod | Ne yapar | Neden gerekli |
| --- | --- | --- |
| `[HttpPost("checkout")]` | Sadece **POST** kabul eder ve route'a `/checkout` ekler | Stripe webhook'ları POST'tur. GET olsaydı tarayıcı ön belleğe alabilir/tekrar çalıştırabilirdi |
| `[AllowAnonymous]` | Bu uç **token istemez** | Stripe'ta bizim JWT'miz yok. Kimlik doğrulaması imza ile yapılır. Ayrıca ileride global bir yetki politikası eklenirse ucun yanlışlıkla kapanmamasını garanti eder |
| `[Consumes("application/json")]` | Gövdenin JSON olduğunu bildirir | Yanlış içerik tipini baştan ayırır; Swagger'da doğru görünür |
| `IActionResult` | `Ok(...)` gibi HTTP sonuçları döndürebilmek için | Cevap gövdesini tip olarak sabitlemek yerine HTTP sonucunu döndürüyoruz |
| `[FromHeader(Name = ...), Required]` | Değeri **HTTP başlığından** alır, adı `Stripe-Signature` | İmza gövdede değil başlıkta gelir. `Required` etiketi niyeti belgeler (projede otomatik 400 kapalı olduğu için gerçek kontrol validator'dadır) |
| `CancellationToken` | Framework tarafından otomatik doldurulur (`RequestAborted`) | İstemci/Stripe bağlantıyı koparırsa akış durur: boşa DB yazımı olmaz |

---

## 1.5 Metot gövdesi (asıl iş)

```csharp
using var reader = new StreamReader(Request.Body);

string payload = await reader.ReadToEndAsync(cancellationToken);

var command = new ProcessStripeWebhookCommand
{
    Signature = signature,
    Payload = payload
};

BaseResponse response = await _mediator.Send(command, cancellationToken);

return Ok(response);
```

| Satır | Ne yapar | Neden böyle yazıldı |
| --- | --- | --- |
| `new StreamReader(Request.Body)` | HTTP isteğinin **ham** gövdesine stream olarak erişir | İmza, gövdenin **bayt bayt aynısı** üzerinden hesaplanır |
| `ReadToEndAsync(ct)` | Gövdenin tamamını tek seferde string'e okur | Tek okuma yeterli; iptal desteğiyle |
| `using` | Reader'ı iş bitince kapatır | Kaynak sızıntısını önler |
| `new ProcessStripeWebhookCommand { ... }` | Handler'a taşınacak **veri** nesnesini kurar | Katmanlar arası sözleşme: controller HTTP'yi bilir, handler işi bilir |
| `_mediator.Send(command, ct)` | Komutu doğru handler'a iletir (arada validator da çalışır) | Controller iş mantığına bağımlı olmaz |
| `Ok(response)` | **200** + `BaseResponse` JSON'u döner | Stripe 2xx görürse olayı tekrar göndermez |

---

## 1.6 Neden `[FromBody]` kullanmadık? (en kritik detay)

```csharp
// YAPMADIK:
public async Task<IActionResult> X([FromBody] ProcessStripeWebhookCommand command)
```

| Sorun | Açıklama |
| --- | --- |
| Gövde **yeniden serileştirilir** | Model binding JSON'u nesneye çevirir; sonra imza doğrulaması için tekrar string'e çevirmek gerekir. Bu dönüşümde **boşluk, alan sırası, kaçış karakterleri** değişebilir |
| İmza tutmaz | İmza o gövdenin **orijinal hâli** için üretilmiştir. Değişen bir string için hesaplanan imza hiçbir zaman eşleşmez → her webhook "geçersiz imza" olur |
| Güvenlik zayıflar | `signature` alanı komutun içinde olsaydı, gövdeye yazılan bir değer doğrulamaya karışabilirdi. Bu yüzden model binding'e kapatıldı (`[BindNever]`, `[JsonIgnore]` — bkz. 02) |

**Özet:** İmzalı veriyle çalışırken gövde **hiç dokunulmadan** taşınmalıdır.

---

## 1.7 Swagger satırları (dokümantasyon)

```csharp
[ProducesResponseType(typeof(BaseResponse), 200)]
[SwaggerResponseExample(StatusCodes.Status200OK, typeof(StripeWebhookProcessedResponseExample))]
[ProducesResponseType(typeof(ErrorResponse), 400)]   // + BadRequestErrorExample
[ProducesResponseType(typeof(ErrorResponse), 404)]   // + NotFoundErrorExample
[ProducesResponseType(typeof(ErrorResponse), 409)]   // + ConflictErrorExample
```

| Kod | Ne yapar |
| --- | --- |
| `ProducesResponseType` | Swagger'a "bu uç şu kodu dönebilir" der |
| `SwaggerResponseExample` | Swagger'da görünecek **örnek JSON** gövdesini bağlar |
| `#region Swagger Docs` | Sadece okunabilirlik için gruplama; derlemeye etkisi yok |

**Neden 404/409 yazılı?** Bu uç, cevabı **operasyon ekibine** de anlatan bir uçtur: "hangi durumda hangi kodu döndüğümüzü" dokümante etmek, ileride hata ayıklamayı (Stripe panelindeki webhook loglarını okumayı) kolaylaştırır.

---

## 1.8 Senin yazdığın stub'dan değişenler

| Önce (stub) | Sonra | Neden |
| --- | --- | --- |
| `public async Task<IActionResult> CheckoutWebhookRoute()` — gövdesi boş | Parametreler + gövde dolduruldu | Uç gerçekten çalışsın |
| — | `[AllowAnonymous]` eklendi | Stripe token göndermez |
| — | `Stripe-Signature` başlığı + `CancellationToken` eklendi | İmza ve iptal desteği |
| — | Ham gövde okuma eklendi | İmza doğrulaması için şart |
| — | `_mediator.Send(command)` eklendi | İş mantığına devir |
| `using RestaurantApi.Application.Common.Abstractions;` | Kaldırıldı | Kullanılmıyordu (`ICurrentUserService` bu uçta yok) |
| — | Swagger attribute'ları + XML dokümantasyon eklendi | Projedeki diğer uçlarla tutarlılık |

[→ 02 — Command ve Validator](02-command-ve-validator.md)