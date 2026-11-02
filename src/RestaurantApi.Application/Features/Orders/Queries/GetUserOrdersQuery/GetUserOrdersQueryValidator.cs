using FluentValidation;

namespace RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

public sealed class GetUserOrdersQueryValidator: AbstractValidator<GetUserOrdersQuery>
{
    public GetUserOrdersQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur ve boş olamaz.");
    }
}
