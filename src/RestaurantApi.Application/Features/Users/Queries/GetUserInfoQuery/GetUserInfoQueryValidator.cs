using FluentValidation;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;

public sealed class GetUserInfoQueryValidator: AbstractValidator<GetUserInfoQuery>
{
    public GetUserInfoQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur ve boş olamaz.");
    }
}
