using FluentValidation;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;

public sealed class CreateOnlineCheckoutCommandValidator: AbstractValidator<CreateOnlineCheckoutCommand>
{
    // Order tablosundaki IdempotencyKey kolonu en fazla 150 karakterdir.
    private const int MaxIdempotencyKeyLength = 150;

    public CreateOnlineCheckoutCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Idempotency key zorunludur.")
            .MaximumLength(MaxIdempotencyKeyLength)
            .WithMessage($"Idempotency key en fazla {MaxIdempotencyKeyLength} karakter olabilir.");

        RuleFor(x => x.CheckoutRequestTypes)
            .IsInEnum()
            .WithMessage("Geçersiz ödeme işlem türü.");

        When(x => x.CheckoutRequestTypes == CheckoutRequestTypes.PanelUser, () =>
        {
            RuleFor(x => x.UserId)
                .Null()
                .WithMessage("Panel işlemlerinde kullanıcı ID bilgisi gönderilemez.");

            RuleFor(x => x.RestaurantTableId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("Panel işlemlerinde geçerli bir masa ID bilgisi zorunludur.");
        });

        When(x => x.CheckoutRequestTypes == CheckoutRequestTypes.CurrentUser, () =>
        {
            RuleFor(x => x.RestaurantTableId)
                .Null()
                .WithMessage("Kullanıcı işlemlerinde masa ID bilgisi gönderilemez.");

            RuleFor(x => x.UserId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("Kullanıcı işlemlerinde geçerli bir kullanıcı ID bilgisi zorunludur.");
        });
    }
}