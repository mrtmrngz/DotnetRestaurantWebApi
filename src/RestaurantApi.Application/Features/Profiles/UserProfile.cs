using AutoMapper;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class UserProfile: Profile
{
    public UserProfile()
    {
        CreateMap<AppUser, GetMeQueryResult>();

        // Panel kullanıcı listesi: tam ad, ad ve soyadın birleşimidir (projeksiyonda metot çağrısı SQL'e
        // çevrilemediği için alanlar doğrudan birleştirilir). TwoFactorStatus, TwoFactorEnabled alanından gelir.
        // Roles ise kullanıcı-rol join tablosundan ayrı bir sorguyla doldurulur (bkz. handler).
        CreateMap<AppUser, GetUserListQueryResult>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name + " " + src.Surname))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty))
            .ForMember(dest => dest.TwoFactorStatus, opt => opt.MapFrom(src => src.TwoFactorEnabled))
            .ForMember(dest => dest.Roles, opt => opt.Ignore());
    }
}