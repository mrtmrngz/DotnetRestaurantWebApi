namespace RestaurantApi.Domain.ValueObjects;

public class ProductSnapshot
{
    public string Title { get; set; } = null!;

    // Sipariş anında geçerli olan (indirim uygulanmış) birim fiyat
    public decimal UnitPrice { get; set; }

    // Sipariş anında uygulanan indirim oranı (yüzde), indirim yoksa 0
    public decimal DiscountRate { get; set; }
}