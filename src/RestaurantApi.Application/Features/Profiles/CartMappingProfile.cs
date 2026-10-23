using AutoMapper;
using RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;
using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.Application.Features.Profiles;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        // Get Cart Items
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
        
        // Get Cart Total
        CreateMap<CartTotalProjection, GetCartTotalQueryResult>()
            .ForCtorParam(nameof(GetCartTotalQueryResult.Subtotal),
                opt => opt.MapFrom(src => src.SubTotal))
            .ForCtorParam(nameof(GetCartTotalQueryResult.AfterDiscountTotal),
                opt => opt.MapFrom(src => src.AfterDiscountTotal))
            .ForCtorParam(nameof(GetCartTotalQueryResult.SavingTotal),
                opt => opt.MapFrom(src =>
                    src.SubTotal - src.AfterDiscountTotal))
            .ForCtorParam(nameof(GetCartTotalQueryResult.ShippingCost),
                opt => opt.MapFrom(src =>
                    src.AfterDiscountTotal == 0m || src.AfterDiscountTotal >= 500m
                        ? 0m
                        : 120m))
            .ForCtorParam(nameof(GetCartTotalQueryResult.Total),
                opt => opt.MapFrom(src =>
                    src.AfterDiscountTotal +
                    (src.AfterDiscountTotal == 0m || src.AfterDiscountTotal >= 500m
                        ? 0m
                        : 120m)));
    }
}