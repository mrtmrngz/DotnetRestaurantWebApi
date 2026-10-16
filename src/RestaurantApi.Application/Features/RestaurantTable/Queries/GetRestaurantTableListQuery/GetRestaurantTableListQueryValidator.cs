using FluentValidation;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;

public class GetRestaurantTableListQueryValidator: AbstractValidator<GetRestaurantTableListQuery>
{
    public GetRestaurantTableListQueryValidator()
    {
        RuleFor(x => x.MinGuest)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum misafir sayısı negatif olamaz.")
            .When(x => x.MinGuest.HasValue);

        RuleFor(x => x.MaxGuest)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Maksimum misafir sayısı negatif olamaz.")
            .When(x => x.MaxGuest.HasValue);
    }
}
