using AutoMapper;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;
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

        CreateMap<Domain.Entities.Discount, GetDiscountDetailQueryResult>()
            .ForMember(dest => dest.DiscountRate, opt =>
                opt.MapFrom(src => src.Rate))
            .ForMember(dest => dest.ProductName, opt =>
                opt.MapFrom(src => src.Product.Title))
            .ForMember(dest => dest.ProductPrice, opt =>
                opt.MapFrom(src => !src.Product.IsDeleted && src.EndDate >= DateTime.UtcNow
                    ? src.Product.Price
                    : (decimal?)null))
            .ForMember(dest => dest.DiscountedPrice, opt =>
                opt.MapFrom(src => !src.Product.IsDeleted && src.EndDate >= DateTime.UtcNow
                    ? src.Product.Price - (src.Product.Price * (decimal)src.Rate / 100)
                    : (decimal?)null))
            .ForMember(dest => dest.Status, opt =>
                opt.Ignore());
    }
}
