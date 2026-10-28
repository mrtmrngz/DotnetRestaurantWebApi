using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Services;
using RestaurantApi.Infrastructure.Settings;
using Stripe;

namespace RestaurantApi.UnitTests.Features.Checkout;

// PaymentService dış bir servise (Stripe) gittiği için gerçek ağ çağrısı yapılmaz:
// Stripe.net'in HTTP istemci seam'i (StripeConfiguration.StripeClient + sahte HttpMessageHandler) kullanılır.
// Böylece yalnızca kendi ürettiğimiz davranışlar test edilir: yapılandırma hatası, hata eşlemesi,
// sağlayıcıya gönderilen payload ve dönen oturum bilgilerinin aktarımı.
public class PaymentServiceTests: IDisposable
{
    private static readonly DateTime NowUtc = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
    private const long StripeSessionExpiresAtUnix = 1793000000;

    private readonly IStripeClient? _previousStripeClient = StripeConfiguration.StripeClient;

    public void Dispose()
    {
        // global Stripe istemcisi test sonunda eski haline döndürülür
        StripeConfiguration.StripeClient = _previousStripeClient;
    }

    // ERROR TESTS START

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenSecretKeyIsMissing_ShouldThrowInvalidOperationException()
    {
        var order = CreateOrder(price: 200m, quantity: 1).order;

        var paymentService = CreatePaymentService(new FakeStripeHttpMessageHandler(HttpStatusCode.OK, "{}"),
            secretKey: string.Empty, stripeClientApiKey: "sk_test_fake");

        Func<Task> act = async () => await paymentService.CreateCheckoutSessionAsync(order,
            CheckoutRequestTypes.CurrentUser, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stripe:SecretKey*",
                "Eksik yapılandırma iş kuralı hatası gibi değil, sunucu tarafı hatası olarak dönmelidir.");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenStripeReturnsError_ShouldThrowUnprocessableEntityError()
    {
        var order = CreateOrder(price: 200m, quantity: 1).order;

        const string stripeErrorResponse = """
            {"error":{"type":"invalid_request_error","code":"api_key_invalid","message":"Invalid API Key provided."}}
            """;

        var paymentService = CreatePaymentService(
            new FakeStripeHttpMessageHandler(HttpStatusCode.Unauthorized, stripeErrorResponse));

        Func<Task> act = async () => await paymentService.CreateCheckoutSessionAsync(order,
            CheckoutRequestTypes.CurrentUser, CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityError>()
            .WithMessage("Ödeme oturumu oluşturulamadı. Lütfen daha sonra tekrar deneyiniz.");
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenSessionResponseHasNoUrl_ShouldThrowUnprocessableEntityError()
    {
        var order = CreateOrder(price: 200m, quantity: 1).order;

        const string sessionWithoutUrl = """
            {"id":"cs_test_no_url","object":"checkout.session","expires_at":1793000000,"mode":"payment"}
            """;

        var paymentService = CreatePaymentService(
            new FakeStripeHttpMessageHandler(HttpStatusCode.OK, sessionWithoutUrl));

        Func<Task> act = async () => await paymentService.CreateCheckoutSessionAsync(order,
            CheckoutRequestTypes.CurrentUser, CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityError>()
            .WithMessage("Ödeme oturumu bilgileri alınamadı.");
    }

    // ERROR TESTS END

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenStripeReturnsSuccess_ShouldReturnSessionInformation()
    {
        var order = CreateOrder(price: 200m, quantity: 1).order;

        var paymentService = CreatePaymentService(
            new FakeStripeHttpMessageHandler(HttpStatusCode.OK, CreateSessionResponse()));

        var session = await paymentService.CreateCheckoutSessionAsync(order,
            CheckoutRequestTypes.CurrentUser, CancellationToken.None);

        session.SessionId.Should().Be("cs_test_123");
        session.SessionUrl.Should().Be("https://checkout.stripe.com/c/pay/cs_test_123");
        session.ExpiresAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(StripeSessionExpiresAtUnix).UtcDateTime);
    }

    // SUCCESS TESTS END

    // PAYLOAD TESTS START

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenCurrentUserOrder_ShouldSendDiscountedItemsShippingAndMetadata()
    {
        // %25 indirimli ürün (200 -> 150) x 2 adet = 300 TL, kargo 120 TL, toplam 420 TL
        var order = CreateOrder(price: 200m, quantity: 2, discountRate: 25).order;

        var handler = new FakeStripeHttpMessageHandler(HttpStatusCode.OK, CreateSessionResponse());
        var paymentService = CreatePaymentService(handler);

        await paymentService.CreateCheckoutSessionAsync(order, CheckoutRequestTypes.CurrentUser,
            CancellationToken.None);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().Be("/v1/checkout/sessions");
        handler.LastRequest.Headers.GetValues("Idempotency-Key").Should().ContainSingle()
            .Which.Should().Be(order.IdempotencyKey, "idempotency key sağlayıcıya iletilmelidir.");

        var body = ParseRequestBody(handler.LastRequestBody);

        body["mode"].Should().Be("payment");
        body["expires_at"].Should().Be(new DateTimeOffset(NowUtc.AddHours(1), TimeSpan.Zero).ToUnixTimeSeconds()
            .ToString(), "ödeme oturumu 1 saat geçerli olmalıdır.");

        // ürün kalemi: indirimli birim fiyat kuruş cinsinden gönderilir
        body["line_items[0][price_data][currency]"].Should().Be("try");
        body["line_items[0][price_data][unit_amount]"].Should().Be("15000", "150 TL = 15000 kuruş");
        body["line_items[0][price_data][product_data][name]"].Should().Be("Test Ürün");
        body["line_items[0][quantity]"].Should().Be("2");

        // kargo kalemi: tahsil edilen tutar sipariş toplamına eşit olsun diye ayrıca gönderilir
        body["line_items[1][price_data][unit_amount]"].Should().Be("12000", "120 TL kargo = 12000 kuruş");
        body["line_items[1][price_data][product_data][name]"].Should().Be("Kargo");
        body["line_items[1][quantity]"].Should().Be("1");

        // gönderilen toplam, sipariş toplamına (kuruş) eşit olmalı
        var sentTotal = GetSentTotalInMinorUnits(body);
        sentTotal.Should().Be(42000, $"sipariş toplamı {order.Total} TL olarak hesaplanmıştı.");
        sentTotal.Should().Be((long)(order.Total * 100));

        body["metadata[order_id]"].Should().Be(order.Id.ToString());
        body["metadata[checkout_request_type]"].Should().Be(nameof(CheckoutRequestTypes.CurrentUser));
        body["metadata[user_id]"].Should().Be(order.UserId!.Value.ToString());
        body["metadata[restaurant_table_id]"].Should().Be(string.Empty);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WhenTableOrder_ShouldNotSendShippingLineItem()
    {
        var order = CreateOrder(price: 200m, quantity: 1, isTable: true).order;

        var handler = new FakeStripeHttpMessageHandler(HttpStatusCode.OK, CreateSessionResponse());
        var paymentService = CreatePaymentService(handler);

        await paymentService.CreateCheckoutSessionAsync(order, CheckoutRequestTypes.PanelUser, CancellationToken.None);

        var body = ParseRequestBody(handler.LastRequestBody);

        order.ShippingCost.Should().Be(0m, "masa siparişlerinde kargo alınmaz.");
        body["line_items[0][price_data][unit_amount]"].Should().Be("20000");
        body.Should().NotContainKey("line_items[1][price_data][product_data][name]",
            "kargo kalemi yalnızca kargo tutarı olduğunda eklenir.");

        GetSentTotalInMinorUnits(body).Should().Be(20000);

        body["metadata[checkout_request_type]"].Should().Be(nameof(CheckoutRequestTypes.PanelUser));
        body["metadata[restaurant_table_id]"].Should().Be(order.RestaurantTableId!.Value.ToString());
        body["metadata[user_id]"].Should().Be(string.Empty);
    }

    // PAYLOAD TESTS END

    #region Helper Methods

    // secretKey: StripeSettings'e bağlanan değer; stripeClientApiKey: sahte HTTP istemcisi için geçerli bir anahtar
    // (Stripe istemcisi boş anahtar ile oluşturulamadığı için yapılandırma eksik senaryosunda ayrı verilir).
    private static PaymentService CreatePaymentService(FakeStripeHttpMessageHandler handler,
        string secretKey = "sk_test_fake", string? stripeClientApiKey = null)
    {
        StripeConfiguration.StripeClient = new StripeClient(stripeClientApiKey ?? secretKey,
            httpClient: new SystemNetHttpClient(new HttpClient(handler)));

        return new PaymentService(NullLogger<PaymentService>.Instance,
            Options.Create(new StripeSettings
            {
                SecretKey = secretKey,
                Currency = "try",
                SuccessUrl = "https://example.com/payment/success",
                CancelUrl = "https://example.com/payment/cancel"
            }),
            new StubTimeProvider(NowUtc));
    }

    private static (Order order, Domain.Entities.Cart cart) CreateOrder(decimal price, int quantity,
        int? discountRate = null, bool isTable = false)
    {
        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Test Ürün",
            Slug = $"test-urun-{Guid.NewGuid()}",
            Price = price
        };

        if (discountRate.HasValue)
        {
            product.Discounts.Add(new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Rate = discountRate.Value,
                IsActive = true,
                StartDate = NowUtc.AddDays(-1),
                EndDate = NowUtc.AddDays(1)
            });
        }

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = isTable ? null : Guid.NewGuid(),
            RestaurantTableId = isTable ? Guid.NewGuid() : null
        };

        cart.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = quantity
        });

        var order = Order.CreateFromCart(cart, cart.UserId, cart.RestaurantTableId, "idempotency-key-1", NowUtc,
            OrderStatus.Pending);

        return (order, cart);
    }

    private static string CreateSessionResponse()
    {
        return $$"""
                 {"id":"cs_test_123","object":"checkout.session","url":"https://checkout.stripe.com/c/pay/cs_test_123","expires_at":{{StripeSessionExpiresAtUnix}},"mode":"payment","status":"open","payment_status":"unpaid"}
                 """;
    }

    // Stripe.net istek gövdesini form-encoded gönderir (line_items[0][price_data][unit_amount] gibi).
    private static Dictionary<string, string> ParseRequestBody(string? rawBody)
    {
        rawBody.Should().NotBeNullOrWhiteSpace("Stripe'a istek gövdesi gönderilmelidir.");

        var body = new Dictionary<string, string>();

        foreach (var pair in rawBody!.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace("+", " ")) : string.Empty;

            body[key] = value;
        }

        return body;
    }

    // Gönderilen kalemlerin toplamı (kuruş): sum(unit_amount * quantity)
    private static long GetSentTotalInMinorUnits(Dictionary<string, string> body)
    {
        long total = 0;

        for (var index = 0; body.ContainsKey($"line_items[{index}][price_data][unit_amount]"); index++)
        {
            var unitAmount = long.Parse(body[$"line_items[{index}][price_data][unit_amount]"]);
            var quantity = long.Parse(body[$"line_items[{index}][quantity]"]);

            total += unitAmount * quantity;
        }

        return total;
    }

    private sealed class StubTimeProvider: TimeProvider
    {
        private readonly DateTime _utcNow;

        public StubTimeProvider(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(_utcNow, TimeSpan.Zero);
        }
    }

    private sealed class FakeStripeHttpMessageHandler: HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public FakeStripeHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };
        }
    }

    #endregion
}
