using System.Text.Json;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Domain.ValueObjects;

namespace RestaurantApi.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public decimal ShippingCost { get; set; }
    public string? AddressSnapshot { get; set; }

    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime OrderDate { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public Guid? RestaurantTableId { get; set; }
    public Guid? UserId { get; set; }

    public string IdempotencyKey { get; private set; } = null!;
    public string? CheckoutSessionId { get; private set; }
    public string? CheckoutSessionUrl { get; private set; }
    public DateTime? CheckoutSessionExpiresAt { get; private set; }

    public void CalculateTotals()
    {
        if (!OrderItems.Any())
        {
            throw new InvalidOperationException("Sipariş boş olamaz.");
        }

        SubTotal = OrderItems.Sum(x => x.TotalPrice);
        CalculateShipping();
        Total = SubTotal + ShippingCost;
    }

    private void CalculateShipping()
    {
        if (RestaurantTableId.HasValue)
        {
            ShippingCost = 0;
            return;
        }

        ShippingCost = SubTotal >= 500 ? 0 : 120;
    }

    public static Order CreateFromCart(
        Cart cart,
        Guid? userId,
        Guid? restaurantTableId,
        string idempotencyKey,
        DateTime nowUtc,
        OrderStatus orderStatus)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key bilgisi zorunludur.",
                nameof(idempotencyKey));
        }

        if (userId.HasValue == restaurantTableId.HasValue)
        {
            throw new InvalidOperationException(
                "Sipariş yalnızca bir kullanıcıya veya bir masaya ait olmalıdır.");
        }

        if (userId == Guid.Empty || restaurantTableId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçersiz kullanıcı veya masa ID bilgisi.");
        }

        if (!cart.CartItems.Any())
        {
            throw new InvalidOperationException("Sepet boş.");
        }

        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            OrderDate = nowUtc,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = orderStatus,
            UserId = userId,
            RestaurantTableId = restaurantTableId
        };

        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product is null)
            {
                throw new InvalidOperationException(
                    "Sepetteki ürün bilgileri yüklenmemiş.");
            }

            if (cartItem.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Sipariş ürün miktarı sıfırdan büyük olmalıdır.");
            }

            // sipariş anında geçerli (aktif ve tarih aralığında) indirim varsa indirimli birim fiyat kullanılır
            var discountRate = GetActiveDiscountRate(cartItem.Product, nowUtc);
            var unitPrice = CalculateEffectiveUnitPrice(cartItem.Product.Price, discountRate);

            var productSnapshot = new ProductSnapshot
            {
                Title = cartItem.Product.Title,
                UnitPrice = unitPrice,
                DiscountRate = discountRate
            };

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = newOrder.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Title,
                ProductSnapshot = JsonSerializer.Serialize(productSnapshot),
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity
            };

            orderItem.CalculateTotal();
            newOrder.OrderItems.Add(orderItem);
        }

        newOrder.CalculateTotals();

        return newOrder;
    }

    // Ürünün verilen an için geçerli indirim oranı (yüzde): hem aktif hem de tarih aralığında olmalıdır.
    private static decimal GetActiveDiscountRate(Product product, DateTime nowUtc)
    {
        return product.Discounts
            .Where(d => d.IsActive && d.StartDate <= nowUtc && d.EndDate >= nowUtc)
            .Select(d => (decimal?)d.Rate)
            .FirstOrDefault() ?? 0m;
    }

    // İndirimli birim fiyat: fiyat * (1 - oran / 100), para birimi için 2 haneye yuvarlanır.
    private static decimal CalculateEffectiveUnitPrice(decimal price, decimal discountRate)
    {
        if (discountRate <= 0m)
            return price;

        if (discountRate >= 100m)
            return 0m;

        return Math.Round(price * (1m - discountRate / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public void HandleSetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!UserId.HasValue || UserId.Value != address.UserId)
        {
            throw new InvalidOperationException(
                "Adres, siparişi oluşturan kullanıcıya ait olmalıdır.");
        }

        var snapshot = new RestaurantApi.Domain.ValueObjects.AddressSnapshot
        {
            UserId = address.UserId,
            Id = address.Id,
            RecipientName = address.RecipientName,
            BuildingInfo = address.BuildingInfo,
            BuildingNumber = address.BuildingNumber,
            City = address.City,
            Neighborhood = address.Neighborhood,
            PhoneNumber = address.PhoneNumber,
            Street = address.Street,
            Town = address.Town,
            ZipCode = address.ZipCode,
            FullAddress = address.FullAddress
        };

        AddressSnapshot = JsonSerializer.Serialize(snapshot);
    }

    public void AttachCheckoutSession(
        string sessionId,
        string url,
        DateTime expiresAt)
    {
        if (PaymentStatus != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                "Bu sipariş için checkout session oluşturulamaz.");
        }

        if (string.IsNullOrWhiteSpace(sessionId) ||
            string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "Checkout session bilgileri boş olamaz.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException(
                "Checkout session bitiş zamanı gelecekte olmalıdır.");
        }

        CheckoutSessionId = sessionId;
        CheckoutSessionUrl = url;
        CheckoutSessionExpiresAt = expiresAt;
    }

    public bool MarkPaid()
    {
        if (PaymentStatus != PaymentStatus.Pending)
            return false;

        PaymentStatus = PaymentStatus.Paid;
        return true;
    }

    public bool MarkExpired()
    {
        if (PaymentStatus != PaymentStatus.Pending)
            return false;

        OrderStatus = OrderStatus.Canceled;
        PaymentStatus = PaymentStatus.Failed;

        return true;
    }
}