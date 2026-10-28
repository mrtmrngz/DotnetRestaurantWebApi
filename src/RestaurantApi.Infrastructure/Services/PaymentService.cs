using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Infrastructure.Settings;
using Stripe;
using Stripe.Checkout;
using Session = Stripe.Checkout.Session;

namespace RestaurantApi.Infrastructure.Services;

public class PaymentService: IPaymentService
{
    // Veritabanında tutarlar ana birimde (TRY) tutulur, Stripe en küçük birimi (kuruş) bekler.
    private const long MinorUnitMultiplier = 100;

    // Kargo, sipariş toplamı ile tahsil edilen tutar eşleşsin diye ayrı kalem olarak gönderilir.
    private const string ShippingLineItemName = "Kargo";

    // Ödeme oturumunun geçerlilik süresi (Stripe en az 30 dakika, en fazla 24 saat kabul eder).
    private static readonly TimeSpan CheckoutSessionLifetime = TimeSpan.FromHours(1);

    private readonly ILogger<PaymentService> _logger;
    private readonly StripeSettings _stripeSettings;
    private readonly TimeProvider _timeProvider;

    public PaymentService(ILogger<PaymentService> logger, IOptions<StripeSettings> stripeSettings,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, CheckoutRequestTypes requestType,
        CancellationToken ctx)
    {
        ArgumentNullException.ThrowIfNull(order);

        // yapılandırma hatası iş kuralı hatası gibi (422) görünmesin: eksik anahtar sunucu tarafı hatasıdır (500)
        if (string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
        {
            _logger.LogCritical(
                "Stripe SecretKey yapılandırılmamış. User-secrets içinde 'Stripe:SecretKey' tanımlanmalıdır.");

            throw new InvalidOperationException(
                "Stripe yapılandırması eksik: 'Stripe:SecretKey' değeri bulunamadı.");
        }

        _logger.LogInformation(
            "Stripe ödeme oturumu oluşturuluyor. OrderId:{OrderId}, Total:{Total}, ItemCount:{ItemCount}",
            order.Id, order.Total, order.OrderItems.Count);

        SessionCreateOptions options = BuildSessionOptions(order, requestType);

        RequestOptions requestOptions = new()
        {
            ApiKey = _stripeSettings.SecretKey,
            // aynı sipariş için mükerrer oturum açılmasını engeller
            IdempotencyKey = order.IdempotencyKey
        };

        try
        {
            var sessionService = new SessionService();
            Session session = await sessionService.CreateAsync(options, requestOptions, ctx);

            if (string.IsNullOrWhiteSpace(session.Id) || string.IsNullOrWhiteSpace(session.Url))
            {
                _logger.LogError("Stripe oturum bilgileri eksik döndü. OrderId:{OrderId}", order.Id);
                throw new UnprocessableEntityError("Ödeme oturumu bilgileri alınamadı.");
            }

            _logger.LogInformation(
                "Ödeme oturumu oluşturuldu (SUCCESS). OrderId:{OrderId}, SessionId:{SessionId}, ExpiresAt:{ExpiresAt}",
                order.Id, session.Id, session.ExpiresAt);

            return new PaymentSessionResult(session.Id, session.Url, session.ExpiresAt);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Ödeme oturumu oluşturulamadı (FAILED). OrderId:{OrderId}, Code:{Code}, Message:{Message}",
                order.Id, ex.StripeError?.Code, ex.StripeError?.Message);

            throw new UnprocessableEntityError("Ödeme oturumu oluşturulamadı. Lütfen daha sonra tekrar deneyiniz.");
        }
    }

    #region Helper Methods

    private SessionCreateOptions BuildSessionOptions(Order order, CheckoutRequestTypes requestType)
    {
        // oturum 1 saat geçerli olur; süre dolarsa Stripe ödeme sayfası kapanır
        DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.Add(CheckoutSessionLifetime);

        return new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = _stripeSettings.SuccessUrl,
            CancelUrl = _stripeSettings.CancelUrl,
            ClientReferenceId = order.Id.ToString(),
            Locale = "tr",
            ExpiresAt = expiresAt,
            LineItems = BuildLineItems(order),
            Metadata = BuildMetadata(order, requestType)
        };
    }

    private List<SessionLineItemOptions> BuildLineItems(Order order)
    {
        var lineItems = order.OrderItems.Select(BuildLineItem).ToList();

        if (order.ShippingCost > 0)
        {
            _logger.LogInformation("Siparişe kargo kalemi ekleniyor. OrderId:{OrderId}, ShippingCost:{ShippingCost}",
                order.Id, order.ShippingCost);

            lineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = _stripeSettings.Currency,
                    UnitAmount = ToMinorUnits(order.ShippingCost),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = ShippingLineItemName
                    }
                }
            });
        }

        return lineItems;
    }

    // Stripe'a ürün adı ve birim fiyat (indirimli) gönderilir; tutar kuruş cinsine çevrilir.
    private SessionLineItemOptions BuildLineItem(OrderItem orderItem)
    {
        return new SessionLineItemOptions
        {
            Quantity = orderItem.Quantity,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = _stripeSettings.Currency,
                UnitAmount = ToMinorUnits(orderItem.UnitPrice),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = orderItem.ProductName
                }
            }
        };
    }

    private static Dictionary<string, string> BuildMetadata(Order order, CheckoutRequestTypes requestType)
    {
        return new Dictionary<string, string>
        {
            ["order_id"] = order.Id.ToString(),
            ["checkout_request_type"] = requestType.ToString(),
            ["user_id"] = order.UserId?.ToString() ?? string.Empty,
            ["restaurant_table_id"] = order.RestaurantTableId?.ToString() ?? string.Empty
        };
    }

    // 12,50 TRY -> 1250 kuruş
    private static long ToMinorUnits(decimal amount)
    {
        return (long)Math.Round(amount * MinorUnitMultiplier, MidpointRounding.AwayFromZero);
    }

    #endregion
}
