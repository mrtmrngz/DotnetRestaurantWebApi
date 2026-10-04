using AutoMapper;
using RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Profiles;

public class ProductProfile: Profile
{
    public ProductProfile()
    {
        // Public Product List Profile
        CreateMap<Product, PublicProductsListQueryResult>()
            .ForMember(dest => dest.CategoryName, opt =>
                opt.MapFrom(src => src.Category != null ? src.Category.Title : string.Empty))
            .ForMember(dest => dest.Banner, opt =>
                opt.MapFrom(src => src.ProductMedias
                    .Where(pm => pm.IsMain)
                    .Select(pm => pm.Media.Url)
                    .FirstOrDefault() ?? string.Empty))
            .ForMember(dest => dest.Discount, opt =>
            opt.MapFrom(src => src.Discounts
                .Where(d => d.IsActive && d.EndDate > DateTime.UtcNow)
                .Select(d => new ProductActiveDiscount
                {
                    Id = d.Id,
                    DiscountRate = d.Rate
                })
                .FirstOrDefault()));
        
        // Admin Product List Profile
        CreateMap<Product, AdminProductListQueryResult>()
            .ForMember(dest => dest.CategoryName, opt =>
                opt.MapFrom(src => src.Category != null ? src.Category.Title : string.Empty))
            .ForMember(dest => dest.Banner, opt =>
                opt.MapFrom(src => src.ProductMedias
                    .Where(pm => pm.IsMain)
                    .Select(pm => pm.Media.Url)
                    .FirstOrDefault() ?? string.Empty))
            .ForMember(dest => dest.Discount, opt =>
                opt.MapFrom(src => src.Discounts
                    .Where(d => d.IsActive && d.EndDate > DateTime.UtcNow)
                    .Select(d => new ProductActiveDiscount
                    {
                        Id = d.Id,
                        DiscountRate = d.Rate
                    })
                    .FirstOrDefault()));
    }
}