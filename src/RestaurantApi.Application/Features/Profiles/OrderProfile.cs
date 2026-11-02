using AutoMapper;
using RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;
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

        // Panel sipariş listesi: ek olarak sipariş sahibi (userId/tableId) ve sahip tipi döner.
        CreateMap<Domain.Entities.Order, GetAllOrdersQueryResult>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TableId, opt => opt.MapFrom(src => src.RestaurantTableId))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src =>
                src.UserId.HasValue ? GetAllOrdersQueryResult.OwnerUser : GetAllOrdersQueryResult.OwnerTable));

        // Kullanıcı sipariş detayı: sipariş kalemleri yalnızca istenen alanlarla (ürün adı, birim fiyat, adet) eşlenir;
        // AddressSnapshot, SubTotal, ShippingCost, Total ve durum alanları isim eşleşmesiyle gelir.
        CreateMap<Domain.Entities.Order, GetUserOrderDetailQueryResult>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems
                .Select(orderItem => new OrderItemDetail
                {
                    ProductName = orderItem.ProductName,
                    UnitPrice = orderItem.UnitPrice,
                    Quantity = orderItem.Quantity
                })));
    }
}

