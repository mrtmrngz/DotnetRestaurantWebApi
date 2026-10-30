using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;

// Sipariş iptal komutunun şekil doğrulaması: tüm alanlar zorunludur.
public class OrderCancelCommandValidator: AbstractValidator<OrderCancelCommand>
{
    public OrderCancelCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur.");

        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Sipariş ID bilgisi zorunludur.");
    }
}
