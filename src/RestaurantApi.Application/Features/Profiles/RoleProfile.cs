using AutoMapper;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;
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

        // Rol izin listesi: izin satırları (id, name, description) isim eşleşmesiyle döner.
        CreateMap<Permission, GetRolePermissionsQueryResult>();
    }
}
