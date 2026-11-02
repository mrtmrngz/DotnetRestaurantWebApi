using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;

// Panel sipariş detay sorgusunun şekil doğrulaması: sipariş ID zorunludur ve boş olamaz.
public sealed class GetPanelOrderDetailQueryValidator: AbstractValidator<GetPanelOrderDetailQuery>
{
    public GetPanelOrderDetailQueryValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Sipariş ID bilgisi zorunludur ve boş olamaz.");
    }
}
