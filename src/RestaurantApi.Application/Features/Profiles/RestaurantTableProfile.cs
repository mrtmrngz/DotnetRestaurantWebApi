using AutoMapper;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class RestaurantTableProfile: Profile
{
    public RestaurantTableProfile()
    {
        // Admin Restaurant Table List Profile
        CreateMap<Domain.Entities.RestaurantTable, GetRestaurantTableListQueryResult>()
            .ForMember(dest => dest.GuestCount, opt =>
                opt.MapFrom(src => src.Capacity));

        // Admin Restaurant Table Detail Profile
        CreateMap<Domain.Entities.RestaurantTable, GetRestaurantTableDetailQueryResult>()
            .ForMember(dest => dest.GuestCount, opt =>
                opt.MapFrom(src => src.Capacity));
    }
}
