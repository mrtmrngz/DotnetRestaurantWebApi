using FluentValidation;

namespace RestaurantApi.Application.Features.Checkout.Commands.CreateTableCashCheckoutCommand;

public sealed class CreateTableCashCheckoutCommandValidator: AbstractValidator<CreateTableCashCheckoutCommand>
{
    // Order tablosundaki IdempotencyKey kolonu en fazla 150 karakterdir.
    private const int MaxIdempotencyKeyLength = 150;

    public CreateTableCashCheckoutCommandValidator()
    {
        RuleFor(x => x.RestaurantTableId)
            .Must(tableId => tableId != Guid.Empty)
            .WithMessage("Masa ID bilgisi zorunludur ve boş olamaz.");

        RuleFor(x => x.IdempotencyKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Idempotency key zorunludur.")
            .MaximumLength(MaxIdempotencyKeyLength)
            .WithMessage($"Idempotency key en fazla {MaxIdempotencyKeyLength} karakter olabilir.");
    }
}
