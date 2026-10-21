using AutoMapper;
using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class ReservationProfile: Profile
{
    public ReservationProfile()
    {
        CreateMap<Domain.Entities.Reservation, ReservationListQueryResult>()
            .ForMember(dest => dest.RestaurantTableNumber, opt =>
                opt.MapFrom(src => src.RestaurantTable.Number));
    }
}