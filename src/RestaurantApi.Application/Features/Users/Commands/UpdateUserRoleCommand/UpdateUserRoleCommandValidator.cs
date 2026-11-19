using FluentValidation;

namespace RestaurantApi.Application.Features.Users.Commands.UpdateUserRoleCommand;

public class UpdateUserRoleCommandValidator: AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı id boş olamaz");
        
        RuleFor(x => x.RoleIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Rol id'leri zorunludur.")
            .Must(ids => ids.Count > 0).WithMessage("Kullanıcıya en az 1 rol atanmalıdır.");

        RuleForEach(x => x.RoleIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Rol id boş olamaz.")
            .Must(id => Guid.TryParse(id, out var guid) && guid != Guid.Empty)
            .WithMessage("Geçersiz rol id formatı.");

        RuleFor(x => x.RoleIds)
            .Must(ids => ids
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .Distinct()
                .Count() == ids.Count)
            .When(x => x.RoleIds is not null && x.RoleIds.All(id => Guid.TryParse(id, out _)))
            .WithMessage("Rol listesinde tekrar eden değerler var.");
    }
}