using FluentValidation;

namespace RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;

// Rol bilgisi sorgusunun şekil doğrulaması: rol ID zorunludur ve boş olamaz.
// Route kısıtı (`{roleId:guid}`) sayesinde geçersiz bir rol ID'si uca zaten ulaşamaz; bu doğrulama
// ek bir güvenlik katmanıdır ve normal akışta istemciye doğrulama hatası dönmez.
public sealed class GetRoleInfoQueryValidator: AbstractValidator<GetRoleInfoQuery>
{
    public GetRoleInfoQueryValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Rol ID bilgisi zorunludur ve boş olamaz.");
    }
}
