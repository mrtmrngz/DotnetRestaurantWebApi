using FluentValidation;

namespace RestaurantApi.Application.Features.Checkout.Commands.ProcessStripeWebhookCommand;

// Webhook isteğinin şeklini doğrular; imzanın kriptografik doğrulaması handler içinde (IPaymentService) yapılır.
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
