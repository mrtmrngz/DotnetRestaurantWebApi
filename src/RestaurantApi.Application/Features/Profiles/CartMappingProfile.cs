using AutoMapper;
using RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Profiles;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<CartItem, GetCartQueryResult>()
            .ForCtorParam(
                nameof(GetCartQueryResult.Title),
                opt => opt.MapFrom(src => src.Product.Title)
            )
            .ForCtorParam(
                nameof(GetCartQueryResult.UnitPrice),
                opt => opt.MapFrom(src => src.Product.Price)
            )
            .ForCtorParam(
                nameof(GetCartQueryResult.DiscountRate),
                opt => opt.MapFrom(src =>
                    src.Product.Discounts
                        .Where(d =>
                            d.IsActive &&
                            d.StartDate <= DateTime.UtcNow &&
                            d.EndDate >= DateTime.UtcNow)
                        .Select(d => (decimal?)d.Rate)
                        .FirstOrDefault() ?? 0m)
            )
            .ForCtorParam(
                nameof(GetCartQueryResult.SubTotal),
                opt => opt.MapFrom(src =>
                    src.Product.Price * src.Quantity)
            )
            .ForCtorParam(
                nameof(GetCartQueryResult.TotalPrice),
                opt => opt.MapFrom(src =>
                    src.Product.Price * src.Quantity *
                    (1m - (
                        (src.Product.Discounts
                            .Where(d =>
                                d.IsActive &&
                                d.StartDate <= DateTime.UtcNow &&
                                d.EndDate >= DateTime.UtcNow)
                            .Select(d => (decimal?)d.Rate)
                            .FirstOrDefault() ?? 0m) / 100m))
                )
            );
    }
}