using FluentValidation;

namespace RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;

// Kullanıcı rol durumu sorgusunun şekil doğrulaması: kullanıcı ID zorunludur ve boş olamaz.
// ID bir Guid olduğu için "geçerli bir GUID" şartı zaten tip ve route kısıtı (`{userId:guid}`) ile garanti edilir;
// bu doğrulama ek bir güvenlik katmanıdır ve normal akışta istemciye doğrulama hatası dönmez.
public sealed class GetUserRolesQueryValidator: AbstractValidator<GetUserRolesQuery>
{
    public GetUserRolesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID bilgisi zorunludur ve boş olamaz.");
    }
}
