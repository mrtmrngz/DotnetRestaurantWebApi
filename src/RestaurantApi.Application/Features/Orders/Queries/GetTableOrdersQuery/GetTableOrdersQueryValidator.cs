using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;

// Masa sipariş listeleme sorgusunun şekil doğrulaması: masa ID zorunludur ve boş olamaz.
public sealed class GetTableOrdersQueryValidator: AbstractValidator<GetTableOrdersQuery>
{
    public GetTableOrdersQueryValidator()
    {
        RuleFor(x => x.TableId)
            .NotEmpty()
            .WithMessage("Masa ID bilgisi zorunludur ve boş olamaz.");
    }
}
