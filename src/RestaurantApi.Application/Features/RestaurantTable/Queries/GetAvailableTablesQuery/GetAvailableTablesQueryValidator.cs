using FluentValidation;

namespace RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;

public class GetAvailableTablesQueryValidator: AbstractValidator<GetAvailableTablesQuery>
{
    public GetAvailableTablesQueryValidator()
    {
        RuleFor(x => x.GuestCount)
            .NotEmpty()
            .WithMessage("Misafir sayısı zorunludur.")
            .GreaterThan(0)
            .WithMessage("Misafir sayısı 0'dan büyük olmalıdır.");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Başlangıç zamanı zorunludur.");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .WithMessage("Bitiş zamanı zorunludur.");
    }
}
