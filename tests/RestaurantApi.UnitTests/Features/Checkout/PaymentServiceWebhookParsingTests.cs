using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Infrastructure.Services;
using RestaurantApi.Infrastructure.Settings;

namespace RestaurantApi.UnitTests.Features.Checkout;

// Webhook imzası yerel olarak (HMAC-SHA256) doğrulandığı için ağ çağrısı yapılmaz: imza doğrulaması,
// reddetme senaryoları (bozuk gövde, tolerans dışı zaman damgası) ve olay çözümlemesi test edilir.
public class PaymentServiceWebhookParsingTests
{
    private const string WebhookSecret = "whsec_test_fake";

    private static PaymentService CreateService()
    {
        return new PaymentService(NullLogger<PaymentService>.Instance,
            Options.Create(new StripeSettings
            {
                SecretKey = "sk_test_fake",
                WebhookSecret = WebhookSecret,
                Currency = "try",
                SuccessUrl = "https://example.com/success",
                CancelUrl = "https://example.com/cancel"
            }),
            TimeProvider.System);
    }

    // Stripe olay gövdesi şablonu (gerçek olaylardaki alanlarla aynı): yer tutucular test verisiyle doldurulur.
    private const string PayloadTemplate =
        "{\"id\":\"evt_test_1\",\"object\":\"event\",\"type\":\"__TYPE__\",\"data\":{\"object\":{\"id\":\"__SESSION__\",\"object\":\"checkout.session\",\"payment_status\":\"__PAYMENT_STATUS__\",\"metadata\":{\"order_id\":\"__ORDER__\"}}}}";

    private static string BuildPayload(string eventType, string sessionId, string paymentStatus, string orderId)
    {
        return PayloadTemplate
            .Replace("__TYPE__", eventType)
            .Replace("__SESSION__", sessionId)
            .Replace("__PAYMENT_STATUS__", paymentStatus)
            .Replace("__ORDER__", orderId);
    }

    private static string BuildSignature(string payload, long timestamp)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));

        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    [Fact]
    public void ParseWebhookEvent_WithValidSignature_ShouldReturnParsedEvent()
    {
        var orderId = Guid.NewGuid().ToString();
        var payload = BuildPayload("checkout.session.completed", "cs_test_123", "paid", orderId);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var stripeEvent = CreateService().ParseWebhookEvent(payload, BuildSignature(payload, timestamp));

        stripeEvent.EventId.Should().Be("evt_test_1");
        stripeEvent.EventType.Should().Be(StripeWebhookEventTypes.CheckoutSessionCompleted);
        stripeEvent.CheckoutSessionId.Should().Be("cs_test_123");
        stripeEvent.OrderId.Should().Be(orderId);
        stripeEvent.SessionPaymentStatus.Should().Be("paid");
    }

    [Fact]
    public void ParseWebhookEvent_WithInvalidSignature_ShouldThrowBadRequestException()
    {
        var payload = BuildPayload("checkout.session.completed", "cs_test_123", "paid", Guid.NewGuid().ToString());

        Action act = () => CreateService().ParseWebhookEvent(payload, "t=1,v1=deadbeef");

        act.Should().Throw<BadRequestException>().WithMessage("*imzası doğrulanamadı*");
    }

    [Fact]
    public void ParseWebhookEvent_WhenPayloadIsTampered_ShouldThrowBadRequestException()
    {
        var payload = BuildPayload("checkout.session.completed", "cs_test_123", "paid", Guid.NewGuid().ToString());
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = BuildSignature(payload, timestamp);

        // gövde değiştirilirse (imza yeniden hesaplanmadığı için) istek reddedilir
        var tamperedPayload = payload.Replace("cs_test_123", "cs_test_999");

        Action act = () => CreateService().ParseWebhookEvent(tamperedPayload, signature);

        act.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void ParseWebhookEvent_WhenTimestampIsOutsideTolerance_ShouldThrowBadRequestException()
    {
        var payload = BuildPayload("checkout.session.completed", "cs_test_123", "paid", Guid.NewGuid().ToString());

        // bir saat önce üretilmiş imza tolerans (300 sn) dışındadır: replay denemesi reddedilir
        long timestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();

        Action act = () => CreateService().ParseWebhookEvent(payload, BuildSignature(payload, timestamp));

        act.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void ParseWebhookEvent_WhenWebhookSecretIsMissing_ShouldThrowInvalidOperationException()
    {
        var service = new PaymentService(NullLogger<PaymentService>.Instance,
            Options.Create(new StripeSettings
            {
                SecretKey = "sk_test_fake",
                WebhookSecret = string.Empty,
                Currency = "try",
                SuccessUrl = "https://example.com/success",
                CancelUrl = "https://example.com/cancel"
            }),
            TimeProvider.System);

        Action act = () => service.ParseWebhookEvent("{}", "t=1,v1=abc");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Stripe:WebhookSecret*");
    }
}
