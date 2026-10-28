using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Dtos.PaymentDtos;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.IntegrationTests.Setup;

// Testlerde gerçek Stripe çağrısı yapılmaz; ödeme oturumu deterministik olarak üretilir.
public class FakePaymentService: IPaymentService
{
    public const string SessionId = "cs_test_fake_session";
    public const string SessionUrl = "https://checkout.stripe.com/c/pay/cs_test_fake_session";

    public Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, CheckoutRequestTypes requestType,
        CancellationToken ctx)
    {
        return Task.FromResult(new PaymentSessionResult(SessionId, SessionUrl, DateTime.UtcNow.AddHours(1)));
    }
}
