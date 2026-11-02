using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;

// Sipariş detay sorgusunun şekil doğrulaması: sipariş ve kullanıcı ID'leri zorunludur ve boş olamaz.
public sealed class GetUserOrderDetailQueryValidator: AbstractValidator<GetUserOrderDetailQuery>
{
    public GetUserOrderDetailQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Sipariş ID bilgisi zorunludur ve boş olamaz.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur ve boş olamaz.");
    }
}
