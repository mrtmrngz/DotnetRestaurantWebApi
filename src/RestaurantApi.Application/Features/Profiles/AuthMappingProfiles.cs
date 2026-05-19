using AutoMapper;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class AuthMappingProfiles: Profile
{
    public AuthMappingProfiles()
    {
        CreateMap<RegisterCommand, AppUser>();
    }
}