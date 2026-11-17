using FluentValidation;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    private const int RoleNameMaxLength = 256;

    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Rol adı zorunludur.")
            .MaximumLength(RoleNameMaxLength)
            .WithMessage($"Rol adı en fazla {RoleNameMaxLength} karakter olabilir.");
    }
}
