using FluentValidation;

namespace RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;

public class AddDiscountCommandValidator: AbstractValidator<AddDiscountCommand>
{
    public AddDiscountCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Ürün id boş olamaz.");

        RuleFor(x => x.DiscountRate)
            .GreaterThanOrEqualTo(0.1)
            .WithMessage("İndirim oranı en az 0.1 olmalıdır.")
            .LessThanOrEqualTo(99.9)
            .WithMessage("İndirim oranı en fazla 99.9 olabilir.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("İndirim başlangıç tarihi zorunludur.");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("İndirim bitiş tarihi zorunludur.");
    }
}
