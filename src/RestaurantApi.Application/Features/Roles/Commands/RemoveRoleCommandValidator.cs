using FluentValidation;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Rol ID bilgisi zorunludur ve boş olamaz.");
    }
}
