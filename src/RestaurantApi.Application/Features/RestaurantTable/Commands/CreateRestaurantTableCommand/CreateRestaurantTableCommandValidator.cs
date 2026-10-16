using FluentValidation;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;

public class CreateRestaurantTableCommandValidator: AbstractValidator<CreateRestaurantTableCommand>
{
    private const int MaxTableCapacity = 15;

    public CreateRestaurantTableCommandValidator()
    {
        RuleFor(x => x.Number)
            .GreaterThan(0)
            .WithMessage("Masa numarası 0'dan büyük olmalıdır.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0)
            .WithMessage("Masa kapasitesi 0'dan büyük olmalıdır.")
            .LessThan(MaxTableCapacity)
            .WithMessage($"Masa kapasitesi {MaxTableCapacity}'ten küçük olmalıdır.");
    }
}
