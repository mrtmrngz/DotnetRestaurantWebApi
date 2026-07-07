using AutoMapper;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Application.Features.Profiles;

public class UserProfile: Profile
{
    public UserProfile()
    {
        CreateMap<AppUser, GetMeQueryResult>();
    }
}