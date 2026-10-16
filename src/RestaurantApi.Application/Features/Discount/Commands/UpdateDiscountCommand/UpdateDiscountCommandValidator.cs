using FluentValidation;

namespace RestaurantApi.Application.Features.Discount.Commands.UpdateDiscountCommand;

public class UpdateDiscountCommandValidator: AbstractValidator<UpdateDiscountCommand>
{
    public UpdateDiscountCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldToUpdate)
            .WithMessage("Güncellenecek en az bir alan belirtilmelidir.");

        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0.1)
            .WithMessage("İndirim oranı en az 0.1 olmalıdır.")
            .LessThanOrEqualTo(99.9)
            .WithMessage("İndirim oranı en fazla 99.9 olabilir.")
            .When(x => x.Rate.HasValue);

        RuleFor(x => x.StartDate)
            .Must(startDate => startDate.HasValue && startDate.Value != default(DateTime))
            .WithMessage("İndirim başlangıç tarihi boş olamaz.")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .Must(endDate => endDate.HasValue && endDate.Value != default(DateTime))
            .WithMessage("İndirim bitiş tarihi boş olamaz.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.ProductId)
            .Must(productId => productId.HasValue && productId.Value != Guid.Empty)
            .WithMessage("Ürün id boş olamaz.")
            .When(x => x.ProductId.HasValue);
    }

    private static bool HasAtLeastOneFieldToUpdate(UpdateDiscountCommand command)
    {
        return command.Rate.HasValue
               || command.StartDate.HasValue
               || command.EndDate.HasValue
               || command.IsActive.HasValue
               || command.ProductId.HasValue;
    }
}
