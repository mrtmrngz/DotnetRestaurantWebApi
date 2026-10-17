using FluentValidation;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;

public class GetRestaurantTableDetailQueryValidator: AbstractValidator<GetRestaurantTableDetailQuery>
{
    public GetRestaurantTableDetailQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Masa id boş olamaz.");
    }
}
