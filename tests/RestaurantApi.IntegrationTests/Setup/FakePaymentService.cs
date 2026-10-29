using System.Text.Json;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.IntegrationTests.Setup;

// Testlerde gerçek Stripe çağrısı yapılmaz; ödeme oturumu ve webhook imza doğrulaması sahtelenir.
public class FakePaymentService: IPaymentService
{
    public const string SessionId = "cs_test_fake_session";
    public const string SessionUrl = "https://checkout.stripe.com/c/pay/cs_test_fake_session";

    // Testlerde geçerli sayılan imza; InvalidSignature gönderilirse gerçek servisteki gibi 400 döner.
    public const string ValidSignature = "t=1,v1=fake_valid_signature";
    public const string InvalidSignature = "t=1,v1=fake_invalid_signature";

    public Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, CheckoutRequestTypes requestType,
        CancellationToken ctx)
    {
        return Task.FromResult(new PaymentSessionResult(SessionId, SessionUrl, DateTime.UtcNow.AddHours(1)));
    }

    // Gerçek servis Stripe imzasını HMAC ile doğrular; burada imza doğrulaması deterministik olarak sahtelenir
    // ve olay gövdesi gerçek Stripe olaylarındaki alanlardan (type, data.object.id / metadata / payment_status) okunur.
    public StripeWebhookEvent ParseWebhookEvent(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || signature == InvalidSignature)
        {
            throw new BadRequestException("Webhook imzası doğrulanamadı.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);

            JsonElement root = document.RootElement;

            JsonElement session = default;

            bool hasSession = root.TryGetProperty("data", out JsonElement data)
                              && data.TryGetProperty("object", out session);

            return new StripeWebhookEvent
            {
                EventId = GetStringValue(root, "id") ?? string.Empty,
                EventType = MapEventType(GetStringValue(root, "type")),
                CheckoutSessionId = hasSession ? GetStringValue(session, "id") : null,
                OrderId = hasSession ? GetMetadataValue(session, "order_id") : null,
                SessionPaymentStatus = hasSession ? GetStringValue(session, "payment_status") : null
            };
        }
        catch (JsonException)
        {
            throw new BadRequestException("Webhook gövdesi çözümlenemedi.");
        }
    }

    private static StripeWebhookEventTypes MapEventType(string? eventType)
    {
        return eventType switch
        {
            "checkout.session.completed" => StripeWebhookEventTypes.CheckoutSessionCompleted,
            "checkout.session.async_payment_succeeded" => StripeWebhookEventTypes.CheckoutSessionAsyncPaymentSucceeded,
            "checkout.session.async_payment_failed" => StripeWebhookEventTypes.CheckoutSessionAsyncPaymentFailed,
            "checkout.session.expired" => StripeWebhookEventTypes.CheckoutSessionExpired,
            _ => StripeWebhookEventTypes.Unknown
        };
    }

    private static string? GetStringValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? GetMetadataValue(JsonElement session, string key)
    {
        return session.TryGetProperty("metadata", out JsonElement metadata)
               && metadata.ValueKind == JsonValueKind.Object
            ? GetStringValue(metadata, key)
            : null;
    }
}

