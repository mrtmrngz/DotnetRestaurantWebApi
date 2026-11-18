using AutoMapper;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;
using RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class RoleProfile: Profile
{
    public RoleProfile()
    {
        // Rol listesi: Id, Name ve IsSystemRole entity alanlarından gelir (Name boş olabilir, boş string'e düşürülür).
        // PermissionCount rol-izin eşleşmelerinden sayılır. UserCount ise rolün kullanıcı-rol join tablosundaki
        // kayıtlarından geldiği için projeksiyonda boş bırakılır ve handler tarafından tek seferde doldurulur.
        CreateMap<AppRole, GetRoleListQueryResult>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.PermissionCount, opt => opt.MapFrom(src => src.RolePermissions.Count))
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src => src.IsSystemRole))
            .ForMember(dest => dest.UserCount, opt => opt.Ignore());

        // Kullanıcı rol durumu listesi: Id ve Name kolonlardan gelir; HasRole, kullanıcının rol adlarıyla
        // karşılaştırılarak handler tarafından işaretlenir.
        CreateMap<AppRole, GetUserRolesQueryResult>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.HasRole, opt => opt.Ignore());

        // Rol izin listesi: izin satırları (id, name, description) isim eşleşmesiyle döner.
        CreateMap<Permission, GetRolePermissionsQueryResult>();

        // Rol bilgisi detayı: Id ve IsSystemRole kolonlardan gelir; ad alanları veritabanında boş
        // olabileceği için boş string'e düşürülür.
        CreateMap<AppRole, GetRoleInfoQueryResult>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name ?? string.Empty))
            .ForMember(dest => dest.NormalizedName, opt => opt.MapFrom(src => src.NormalizedName ?? string.Empty))
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src => src.IsSystemRole));
    }
}
