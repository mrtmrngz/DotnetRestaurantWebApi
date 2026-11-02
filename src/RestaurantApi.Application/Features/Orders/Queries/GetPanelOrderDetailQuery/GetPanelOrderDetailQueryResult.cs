using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;

// Panel sipariş detayının sözleşmesi: sipariş özeti + sipariş sahibi bilgisi + sipariş kalemleri.
public record GetPanelOrderDetailQueryResult()
{
    public Guid OrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    // Sipariş anındaki adresin JSON anlık görüntüsü; kullanıcı siparişlerinde zorunlu olduğu için doludur,
    // masa siparişlerinde boş olabilir.
    public string? AddressSnapshot { get; set; }

    // Masa siparişlerinde masa numarası doludur; kullanıcı siparişlerinde boştur.
    public int? TableNumber { get; set; }

    // Kullanıcı siparişlerinde müşterinin tam adı doludur (kullanıcı silinmişse boş döner);
    // masa siparişlerinde boştur.
    public string? CustomerFullName { get; set; }

    public List<PanelOrderItemDetail> OrderItems { get; set; } = new List<PanelOrderItemDetail>();
}

// Panel detayındaki her bir kalem: ürün adı, birim fiyat ve adet.
public record PanelOrderItemDetail()
{
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
