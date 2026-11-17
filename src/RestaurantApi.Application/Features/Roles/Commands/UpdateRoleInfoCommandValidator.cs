using FluentValidation;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed class UpdateRoleInfoCommandValidator : AbstractValidator<UpdateRoleInfoCommand>
{
    // AppRole.Name kolonu varchar(256) olduğu için üst sınır burada da doğrulanır.
    private const int RoleNameMaxLength = 256;

    public UpdateRoleInfoCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role id zorunludur.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(RoleNameMaxLength)
            .WithMessage($"Rol adı en fazla {RoleNameMaxLength} karakter olabilir.");
    }
}
