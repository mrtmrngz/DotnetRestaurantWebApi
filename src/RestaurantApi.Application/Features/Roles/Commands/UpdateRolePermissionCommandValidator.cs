using FluentValidation;

namespace RestaurantApi.Application.Features.Roles.Commands;

public sealed class UpdateRolePermissionCommandValidator : AbstractValidator<UpdateRolePermissionCommand>
{
    public UpdateRolePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role id zorunludur.");

        // Izın listesi: null gelirse zorunluluk hatası döner ve sonraki kontroller çalışmaz
        // (ayrı RuleFor zinciri kurulursa null liste üzerinde tekrar kontrolü çalışıp hata verirdi).
        RuleFor(x => x.PermissionIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("İzin id'leri zorunludur.")
            .Must(ids => ids.Count == ids.Distinct().Count())
            .WithMessage("İzin listesinde tekrar eden değerler var.");

        RuleForEach(x => x.PermissionIds)
            .NotEmpty().WithMessage("İzin id boş olamaz.")
            .When(x => x.PermissionIds is not null);
    }
}
