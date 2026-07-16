using AutoMapper;
using RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class ProfileMappingProfiles : Profile
{
    public ProfileMappingProfiles()
    {
        // Profile Info Mapper START
        CreateMap<AppUser, ProfileInfoQueryResult>()
            .ForMember(dest => dest.TwoFactorStatus, opt => opt.MapFrom(src => src.TwoFactorEnabled));
    }
}