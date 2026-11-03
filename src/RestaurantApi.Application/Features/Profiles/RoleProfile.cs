using AutoMapper;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class RoleProfile: Profile
{
    public RoleProfile()
    {
        // Rol listesi: Id ve Name isim eşleşmesiyle gelir (Name veritabanında boş olabilir, boş string'e düşürülür).
        // PermissionCount rol-izin eşleşmelerinden sayılır, IsSystemRole yalnızca AppRoles içindeki statik roller
        // için true döner. UserCount ise rolün kullanıcı-rol join tablosundaki kayıtlarından geldiği için
        // projeksiyonda boş bırakılır ve handler tarafından tek seferde doldurulur.
        CreateMap<AppRole, GetRoleListQueryResult>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.PermissionCount, opt => opt.MapFrom(src => src.RolePermissions.Count))
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src =>
                src.Name == AppRoles.Admin || src.Name == AppRoles.User))
            .ForMember(dest => dest.UserCount, opt => opt.Ignore());
    }
}
