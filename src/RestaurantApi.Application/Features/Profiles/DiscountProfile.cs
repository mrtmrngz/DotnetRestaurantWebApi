using AutoMapper;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class DiscountProfile: Profile
{
    public DiscountProfile()
    {
        // Admin Discount List Profile
        CreateMap<Domain.Entities.Discount, GetDiscountListQueryResult>()
            .ForMember(dest => dest.DiscountRate, opt =>
                opt.MapFrom(src => src.Rate))
            .ForMember(dest => dest.ProductName, opt =>
                opt.MapFrom(src => src.Product.Title))
            .ForMember(dest => dest.Status, opt =>
                opt.Ignore());
    }
}
