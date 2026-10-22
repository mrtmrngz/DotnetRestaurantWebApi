using FluentValidation;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Features.Cart.Commands.RemoveCartCommand;

public sealed class RemoveCartCommandValidator: AbstractValidator<RemoveCartCommand>
{
    public RemoveCartCommandValidator()
    {
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