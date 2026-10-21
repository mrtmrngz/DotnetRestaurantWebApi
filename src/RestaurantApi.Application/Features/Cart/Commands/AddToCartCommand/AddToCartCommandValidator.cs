using FluentValidation;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;

public sealed class AddToCartCommandValidator: AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Ürün ID bilgisi zorunludur.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 100).WithMessage("Ürün miktarı 1 ile 100 arasında olmak zorundadır.");
        
        RuleFor(x => x.RequestType)
            .IsInEnum()
            .WithMessage("Geçersiz sepet işlem türü.");

        When(x => x.RequestType == CartRequestTypes.PanelUser, () =>
        {
            RuleFor(x => x.UserId)
                .Null()
                .WithMessage("Panel işlemlerinde kullanıcı ID bilgisi gönderilemez.");

            RuleFor(x => x.RestaurantTableId)
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage("Panel işlemlerinde geçerli bir masa ID bilgisi zorunludur.");
        });

        When(x => x.RequestType == CartRequestTypes.CurrentUser, () =>
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