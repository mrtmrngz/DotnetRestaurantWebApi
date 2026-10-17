using FluentValidation;

namespace RestaurantApi.Application.Features.RestaurantTable.Commands.UpdateRestaurantTableCommand;

public class UpdateRestaurantTableCommandValidator: AbstractValidator<UpdateRestaurantTableCommand>
{
    private const int MaxTableCapacity = 15;

    public UpdateRestaurantTableCommandValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneFieldToUpdate)
            .WithMessage("Güncellenecek en az bir alan belirtilmelidir.");

        RuleFor(x => x.Number)
            .GreaterThan(0)
            .WithMessage("Masa numarası 0'dan büyük olmalıdır.")
            .When(x => x.Number.HasValue);

        RuleFor(x => x.Capacity)
            .GreaterThan(0)
            .WithMessage("Masa kapasitesi 0'dan büyük olmalıdır.")
            .LessThan(MaxTableCapacity)
            .WithMessage($"Masa kapasitesi {MaxTableCapacity}'ten küçük olmalıdır.")
            .When(x => x.Capacity.HasValue);
    }

    private static bool HasAtLeastOneFieldToUpdate(UpdateRestaurantTableCommand command)
    {
        return command.Number.HasValue
               || command.Capacity.HasValue
               || command.IsActive.HasValue;
    }
}
