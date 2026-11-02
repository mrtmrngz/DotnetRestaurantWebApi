using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;

// Panel sipariş listesinin her bir satırı.
// Kullanıcı ve masa listeleriyle aynı alanları taşır; ek olarak siparişin sahibini
// (userId / tableId) ve sahip tipini (user / table) döner.
public record GetAllOrdersQueryResult()
{
    // Siparişin sahip tipi için izin verilen değerler.
    public const string OwnerUser = "user";
    public const string OwnerTable = "table";

    public Guid OrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public DateTime OrderDate { get; set; }

    // Kullanıcı siparişi değilse null döner.
    public Guid? UserId { get; set; }

    // Masa siparişi değilse null döner.
    public Guid? TableId { get; set; }

    // "user" ya da "table".
    public string Type { get; set; } = null!;
}
