using AutoMapper;
using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

namespace RestaurantApi.Application.Features.Profiles;

public class OrderProfile: Profile
{
    public OrderProfile()
    {
        // Sipariş listesi: OrderId, entity'de Id olarak tutulduğu için eşlenir;
        // SubTotal, ShippingCost, Total, OrderStatus ve OrderDate alanları isim eşleşmesiyle gelir.
        CreateMap<Domain.Entities.Order, GetUserOrdersQueryResult>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id));

        // Masa sipariş listesi: kullanıcı listesiyle aynı alanlar, ayrı sözleşme.
        CreateMap<Domain.Entities.Order, GetTableOrdersQueryResult>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id));
    }
}
