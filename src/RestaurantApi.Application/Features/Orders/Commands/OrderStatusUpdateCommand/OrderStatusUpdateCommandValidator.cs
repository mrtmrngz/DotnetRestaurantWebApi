using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Commands.OrderStatusUpdateCommand;

// Sipariş durum güncelleme komutunun şekil doğrulaması: tüm alanlar zorunludur.
public class OrderStatusUpdateCommandValidator: AbstractValidator<OrderStatusUpdateCommand>
{
    public OrderStatusUpdateCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Sipariş ID bilgisi zorunludur.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur.");

        RuleFor(x => x.NewStatus)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Sipariş durumu zorunludur.")
            .IsInEnum()
            .WithMessage("Geçersiz sipariş durumu.");
    }
}
