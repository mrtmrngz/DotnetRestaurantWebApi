# 02 — Command ve Validator (satır satır)

**Dosyalar:**
`Application/Features/Checkout/Commands/ProcessStripeWebhookCommand/ProcessStripeWebhookCommand.cs`
`.../ProcessStripeWebhookCommandValidator.cs`

---

## 2.1 Command — sadece veri taşır

```csharp
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RestaurantApi.Application.Models.Responses.SuccessResponse;

namespace RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;

public record ProcessStripeWebhookCommand : IRequest<BaseResponse>
{
    [BindNever]
    [JsonIgnore]
    public string Signature { get; init; } = null!;

    [BindNever]
    [JsonIgnore]
    public string Payload { get; init; } = null!;
}
```

| Kod | Ne yapar | Neden gerekli |
| --- | --- | --- |
| `record` | Değiştirilemez (immutable) veri taşıyıcı; değer eşitliği hazır gelir | Komut bir "mesaj"dır: oluşturulur, okunur, bitir. Sonradan değişmemesi hata riskini azaltır |
| `: IRequest<BaseResponse>` | "Bu komut işlenince `BaseResponse` dönecek" sözleşmesi | MediatR hangi handler'ın çağrılacağını bu tiple bulur |
| `Signature` | `Stripe-Signature` başlığının değeri | İmza doğrulaması bununla yapılır |
| `Payload` | HTTP gövdesinin **ham** hâli | İmza doğrulaması bununla yapılır |
| `[BindNever]` | Bu alanın **model binding ile istekten doldurulmasını yasaklar** | Saldırgan gövdeye `signature` yazıp doğrulamayı atlatmaya çalışabilir. `[BindNever]` bu yolu kapatır |
| `[JsonIgnore]` | (De)serileştirmede bu alanı yok sayar | Komut bir yerde JSON'a çevrilirse imza değeri **loglara/çıktıya sızmasın** |
| `{ get; init; }` | Yalnızca nesne kurulurken set edilir | Alanlar sonradan değiştirilemez: doğrulanan imza ile kullanılan imza aynı kalır |
| `= null!` | "Null olmayacak" der, derleyici uyarısını susturur | Değeri controller doldurur (validator boş gelirse yakalar) |

**Not:** İki alan da **istek gövdesinden gelmez**; biri başlıktan, diğeri ham gövdeden controller tarafından elle konur. `[BindNever]` / `[JsonIgnore]` bu niyeti koda yazar.

---

## 2.2 Validator — şekil kontrolü

```csharp
public sealed class ProcessStripeWebhookCommandValidator: AbstractValidator<ProcessStripeWebhookCommand>
{
    // Stripe imza başlığı "t=...,v1=..." biçimindedir; üst sınır anormal/şişirilmiş başlıkları engeller.
    private const int MaxSignatureLength = 1024;

    // Checkout oturumu olayları birkaç KB'dir; üst sınır büyük gövdelerle DoS denemesini sınırlar.
    private const int MaxPayloadLength = 1024 * 1024;

    public ProcessStripeWebhookCommandValidator()
    {
        RuleFor(x => x.Signature)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Webhook imzası (Stripe-Signature) zorunludur.")
            .MaximumLength(MaxSignatureLength)
            .WithMessage($"Webhook imzası en fazla {MaxSignatureLength} karakter olabilir.");

        RuleFor(x => x.Payload)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Webhook gövdesi zorunludur.")
            .MaximumLength(MaxPayloadLength)
            .WithMessage("Webhook gövdesi izin verilen boyutu aşıyor.");
    }
}
```

| Kod | Ne yapar | Neden gerekli |
| --- | --- | --- |
| `AbstractValidator<T>` | FluentValidation altyapısı | Proje standardı; validator'lar MediatR pipeline'ında otomatik çalışır |
| `sealed` | Sınıfın miras alınmasını engeller | Gereksiz kalıtımı kapatır; niyet "bu kural seti tekildir" |
| `MaxSignatureLength = 1024` | İmza başlığı için üst sınır | Gerçek imza ~150-200 karakterdir. 1024 üstü başlık **şişirme/DoS denemesi** işaretidir |
| `MaxPayloadLength = 1024 * 1024` | Gövde için 1 MB üst sınır | Gerçek olay birkaç KB'dir. Dev gövde, pahalı imza hesabına girmeden reddedilir (CPU/bellek koruması) |
| `RuleFor(x => x.Signature)` | İmza için kural grubu başlatır | — |
| `.Cascade(CascadeMode.Stop)` | İlk kural patlarsa **sonrakileri çalıştırmaz** | Boş imzada ayrıca "çok uzun" hatası görmek anlamsız; tek, net mesaj döner |
| `.NotEmpty()` | Boş/null/whitespace kontrolü | `[Required]` etiketi projede otomatik 400 üretmediği için (`SuppressModelStateInvalidFilter = true`) boş kontrolü burada yapılmak **zorunda** |
| `.WithMessage("...")` | Hata metnini sabitler (Türkçe) | İstemci/operasyon ekibi tutarlı mesaj görsün |
| `.MaximumLength(...)` | Uzunluk sınırı | Yukarıdaki DoS koruması |

---

## 2.3 Bu validator ne zaman çalışıyor?

```text
mediator.Send(command)
        │
        ▼
ValidationBehavior  ← burada TÜM validator'lar koşar (handler'a girmeden)
        │
        ├── hata varsa → ValidationException → ExceptionMiddleware → 400 + alan listesi
        │
        ▼
ProcessStripeWebhookCommandHandler.Handle(...)
```

**Neden handler'ın içinde değil?** Çünkü "istek biçimi doğru mu?" sorusu **her** komut için aynı şekilde cevaplanır. Ortak yerde tutmak, her handler'da tekrar `if (string.IsNullOrEmpty(...))` yazmaktan kurtarır ve kural setini tek dosyada toplar.

**Kritik ayrım:**

| Kontrol | Nerede | Neden orada |
| --- | --- | --- |
| Alan boş mu? Çok uzun mu? | **Validator** (bu dosya) | Şekil kontrolü; ucuz ve genel |
| İmza **kriptografik olarak** geçerli mi? | **Handler → `IPaymentService`** | Sağlayıcıya özel, maliyetli iş; iş kuralı gerektirir (Stripe SDK'sı gerekir) |

Bu yüzden validator dosyasının başında niyet açıkça yazılıdır:
`// Webhook isteğinin şeklini doğrular; imzanın kriptografik doğrulaması handler içinde (IPaymentService) yapılır.`

[→ 03 — Handler.Handle](03-handler-handle-metodu.md)