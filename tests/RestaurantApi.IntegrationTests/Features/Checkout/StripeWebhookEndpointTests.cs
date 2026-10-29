using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Checkout;

// Stripe webhook ucunun (POST api/webhooks/checkout) uçtan uca davranışı.
// Handler doğrudan çağrılmaz; istekler gerçek HTTP ile gönderilir ve sonuç veritabanından doğrulanır.
// Kapsam: imza doğrulaması, olay yönlendirmesi (masa ve kullanıcı siparişleri), sipariş güncellemesi,
// idempotency ve hata kodları (400 / 404 / 409 / 200 + IGNORED).
public class StripeWebhookEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/webhooks/checkout";
    private const string SignatureHeader = "Stripe-Signature";

    public StripeWebhookEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task Webhook_WhenTableOrderPaymentCompleted_ShouldMarkOrderPaid()
    {
        // Arrange
        var (orderId, tableId, sessionId) = await SeedPendingTableOrderAsync("cs_test_table_paid");
        var payload = BuildPayload("checkout.session.completed", sessionId, "paid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.STRIPE_WEBHOOK_PROCESSED);
        body.Message.Should().Be("Ödeme onaylandı, sipariş ödendi olarak işaretlendi.");

        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.OrderStatus.Should().Be(OrderStatus.Pending,
            "ödeme onayı mutfak akışını değiştirmemelidir.");
        order.RestaurantTableId.Should().Be(tableId, "masa siparişi masaya bağlı kalmalıdır.");
        order.UserId.Should().BeNull("masa siparişinde kullanıcı bilgisi bulunmaz.");
        order.CheckoutSessionId.Should().Be(sessionId, "siparişin ödeme oturumu korunmalıdır.");
    }

    [Fact]
    public async Task Webhook_WhenUserOrderAsyncPaymentSucceeded_ShouldMarkOrderPaid()
    {
        // Arrange
        var (orderId, userId, sessionId) = await SeedPendingUserOrderAsync("cs_test_user_async_paid");
        var payload = BuildPayload("checkout.session.async_payment_succeeded", sessionId, "paid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.STRIPE_WEBHOOK_PROCESSED);
        body.Message.Should().Be("Ödeme onaylandı, sipariş ödendi olarak işaretlendi.");

        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.OrderStatus.Should().Be(OrderStatus.Pending);
        order.UserId.Should().Be(userId, "kullanıcı siparişi kullanıcıya bağlı kalmalıdır.");
        order.RestaurantTableId.Should().BeNull("kullanıcı siparişinde masa bilgisi bulunmaz.");
        order.AddressSnapshot.Should().NotBeNullOrWhiteSpace("adres snapshot'ı webhook ile bozulmamalıdır.");
    }

    [Fact]
    public async Task Webhook_WhenSameEventIsDeliveredTwice_ShouldBeIdempotent()
    {
        // Arrange
        var (orderId, _, sessionId) = await SeedPendingTableOrderAsync("cs_test_idempotent");
        var payload = BuildPayload("checkout.session.completed", sessionId, "paid", orderId);

        // Act
        var firstResponse = await PostAsync(payload, FakePaymentService.ValidSignature);
        var firstBody = await firstResponse.ReadContentAsAsync<BaseResponse>();
        var orderAfterFirst = await GetOrderAsync(orderId);

        var secondResponse = await PostAsync(payload, FakePaymentService.ValidSignature);
        var secondBody = await secondResponse.ReadContentAsAsync<BaseResponse>();
        var orderAfterSecond = await GetOrderAsync(orderId);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstBody!.Message.Should().Be("Ödeme onaylandı, sipariş ödendi olarak işaretlendi.");
        orderAfterFirst.PaymentStatus.Should().Be(PaymentStatus.Paid);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "mükerrer olay hata değildir: istenen sonuç zaten sağlanmıştır (idempotent yanıt).");
        secondBody!.Code.Should().Be(Codes.STRIPE_WEBHOOK_PROCESSED);
        secondBody.Message.Should().Be("Olay daha önce işlenmiş, sipariş durumu değiştirilmedi.");
        orderAfterSecond.PaymentStatus.Should().Be(PaymentStatus.Paid, "ödenmiş sipariş geri alınmamalıdır.");
        orderAfterSecond.OrderStatus.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Webhook_WhenUserOrderAsyncPaymentFailed_ShouldCancelOrderAsFailed()
    {
        // Arrange
        var (orderId, _, sessionId) = await SeedPendingUserOrderAsync("cs_test_user_async_failed");
        var payload = BuildPayload("checkout.session.async_payment_failed", sessionId, "unpaid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.STRIPE_WEBHOOK_PROCESSED);
        body.Message.Should().Be("Ödeme başarısız olduğu için sipariş iptal edildi.");

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        order.OrderStatus.Should().Be(OrderStatus.Canceled,
            "ödemesi alınmayan sipariş üretime girmemelidir.");
    }

    [Fact]
    public async Task Webhook_WhenUserOrderSessionExpired_ShouldCancelOrderAsFailed()
    {
        // Arrange
        var (orderId, _, sessionId) = await SeedPendingUserOrderAsync("cs_test_user_expired");
        var payload = BuildPayload("checkout.session.expired", sessionId, "unpaid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Message.Should().Be("Ödeme başarısız olduğu için sipariş iptal edildi.");

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
        order.OrderStatus.Should().Be(OrderStatus.Canceled);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task Webhook_WhenSignatureHeaderIsMissing_ShouldReturn400()
    {
        // Arrange
        var (orderId, _, sessionId) = await SeedPendingTableOrderAsync("cs_test_no_signature");
        var payload = BuildPayload("checkout.session.completed", sessionId, "paid", orderId);

        // Act — imza başlığı bilinçli olarak gönderilmez
        var response = await PostAsync(payload, signature: null);

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body!.Errors.Should().ContainSingle()
            .Which.Message.Should().Be("Webhook imzası (Stripe-Signature) zorunludur.");
        order.PaymentStatus.Should().Be(PaymentStatus.Pending, "imzasız istek hiçbir şey yazmamalıdır.");
    }

    [Fact]
    public async Task Webhook_WhenSignatureIsInvalid_ShouldReturn400AndLeaveOrderPending()
    {
        // Arrange
        var (orderId, _, sessionId) = await SeedPendingTableOrderAsync("cs_test_bad_signature");
        var payload = BuildPayload("checkout.session.completed", sessionId, "paid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.InvalidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "imzası doğrulanamayan istek 'Stripe'tan gelmemiş' kabul edilir.");
        body!.Code.Should().Be(Codes.BAD_REQUEST);
        body.Message.Should().Be("Webhook imzası doğrulanamadı.");

        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
        order.OrderStatus.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Webhook_WhenOrderDoesNotExist_ShouldReturn404()
    {
        // Arrange — başka bir ortamda üretilmiş / silinmiş bir siparişi işaret eden olay
        var payload = BuildPayload("checkout.session.completed", "cs_test_unknown_session", "paid",
            Guid.NewGuid());

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "2xx dönülürse Stripe olayı tekrar göndermez ve ödeme kaydı kaybolur.");
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Webhook olayına ait sipariş bulunamadı.");
    }

    [Fact]
    public async Task Webhook_WhenEventSessionDoesNotMatchOrderSession_ShouldReturn409()
    {
        // Arrange — metadata doğru siparişi gösteriyor ama oturum id'si siparişte kayıtlıdan farklı
        var (orderId, _, _) = await SeedPendingTableOrderAsync("cs_test_conflict_order_session");
        var payload = BuildPayload("checkout.session.completed", "cs_test_conflict_event_session", "paid",
            orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "başka bir oturumun sonucu mevcut siparişe yazılmamalıdır.");
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Webhook olayı siparişin ödeme oturumuyla eşleşmiyor.");
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Webhook_WhenEventTypeIsNotHandled_ShouldReturn200AndIgnore()
    {
        // Arrange — ilgi alanımıza girmeyen bir olay tipi
        var payload = BuildPayload("payment_intent.created", "cs_test_ignored", "paid", Guid.NewGuid());

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "işlenmeyen olayda 2xx dönülür; aksi hâlde Stripe gereksiz yere tekrar dener.");
        body!.Code.Should().Be(Codes.STRIPE_WEBHOOK_IGNORED);
        body.Message.Should().Be("Olay işlenmedi, istek yoksayıldı.");
    }

    [Fact]
    public async Task Webhook_WhenSessionIsCompletedButNotPaid_ShouldReturn200AndIgnore()
    {
        // Arrange — asenkron ödeme yöntemi: oturum tamamlandı ama para henüz alınmadı
        var (orderId, _, sessionId) = await SeedPendingTableOrderAsync("cs_test_completed_unpaid");
        var payload = BuildPayload("checkout.session.completed", sessionId, "unpaid", orderId);

        // Act
        var response = await PostAsync(payload, FakePaymentService.ValidSignature);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.STRIPE_WEBHOOK_IGNORED);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending,
            "ödenmemiş sipariş 'Paid' yazılmamalıdır; sonuç asenkron olayla kesinleşir.");
    }

    #endregion

    // SETUP

    private sealed record OrderState(
        PaymentStatus PaymentStatus,
        OrderStatus OrderStatus,
        Guid? UserId,
        Guid? RestaurantTableId,
        string? CheckoutSessionId,
        string? AddressSnapshot);

    private async Task<HttpResponseMessage> PostAsync(string payload, string? signature)
    {
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, Route)
        {
            Content = content
        };

        // imza başlığı yalnızca gönderilmek istendiğinde eklenir (eksik imza senaryosu için null)
        if (signature is not null)
        {
            request.Headers.Add(SignatureHeader, signature);
        }

        return await Client.SendAsync(request);
    }

    // Stripe olay gövdesi şablonu (gerçek olaylardaki alanlarla aynı): yer tutucular test verisiyle doldurulur.
    private const string PayloadTemplate =
        "{\"id\":\"evt_test\",\"object\":\"event\",\"type\":\"__TYPE__\",\"data\":{\"object\":{\"id\":\"__SESSION__\",\"object\":\"checkout.session\",\"payment_status\":\"__PAYMENT_STATUS__\",\"metadata\":{\"order_id\":\"__ORDER__\"}}}}";

    private static string BuildPayload(string eventType, string sessionId, string paymentStatus, Guid orderId)
    {
        return PayloadTemplate
            .Replace("__TYPE__", eventType)
            .Replace("__SESSION__", sessionId)
            .Replace("__PAYMENT_STATUS__", paymentStatus)
            .Replace("__ORDER__", orderId.ToString());
    }

    private async Task<OrderState> GetOrderAsync(Guid orderId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .AsNoTracking()
            .FirstAsync(x => x.Id == orderId);

        return new OrderState(order.PaymentStatus, order.OrderStatus, order.UserId, order.RestaurantTableId,
            order.CheckoutSessionId, order.AddressSnapshot);
    }

    // Masa (PanelUser) siparişi: sipariş masaya bağlıdır; kullanıcı ve adres bilgisi bulunmaz.
    private async Task<(Guid OrderId, Guid TableId, string SessionId)> SeedPendingTableOrderAsync(string sessionId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = Random.Shared.Next(1, 10_000),
            Capacity = 4,
            IsActive = true
        };

        context.RestaurantTables.Add(table);

        var product = CreateProduct();
        AddProductWithCategory(context, product);

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = table.Id
        };

        cart.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = 1
        });

        var order = Domain.Entities.Order.CreateFromCart(cart, null, table.Id, $"key-{Guid.NewGuid()}",
            DateTime.UtcNow, OrderStatus.Pending);

        order.AttachCheckoutSession(sessionId, $"https://checkout.stripe.com/c/pay/{sessionId}",
            DateTime.UtcNow.AddHours(1));

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return (order.Id, table.Id, sessionId);
    }

    // Kullanıcı (CurrentUser) siparişi: sipariş kullanıcıya bağlıdır ve adres snapshot'ı taşır.
    private async Task<(Guid OrderId, Guid UserId, string SessionId)> SeedPendingUserOrderAsync(string sessionId)
    {
        var userSetup = await CreateVanillaUserAsync();
        var userId = userSetup.User.Id;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = CreateProduct();
        AddProductWithCategory(context, product);

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        cart.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = 1
        });

        var order = Domain.Entities.Order.CreateFromCart(cart, userId, null, $"key-{Guid.NewGuid()}",
            DateTime.UtcNow, OrderStatus.Pending);

        order.HandleSetAddress(CreateAddress(userId));

        order.AttachCheckoutSession(sessionId, $"https://checkout.stripe.com/c/pay/{sessionId}",
            DateTime.UtcNow.AddHours(1));

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return (order.Id, userId, sessionId);
    }

    private static Domain.Entities.Product CreateProduct()
    {
        return new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Webhook Test Ürün",
            Description = "Webhook testleri için ürün",
            Slug = $"webhook-test-{Guid.NewGuid()}",
            Price = 150m,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };
    }

    private static void AddProductWithCategory(ApiContext context, Domain.Entities.Product product)
    {
        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Webhook Test Kategori",
            Slug = $"webhook-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);
    }

    private static Domain.Entities.Address CreateAddress(Guid userId)
    {
        return new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            Title = "Ev",
            RecipientName = "Webhook Test Kullanıcı",
            City = "İstanbul",
            Town = "Şişli",
            Neighborhood = "Test Mahallesi",
            Street = "Test Sokak",
            BuildingNumber = "1",
            PhoneNumber = "5551112233",
            IsDefault = true,
            IsDeleted = false,
            UserId = userId
        };
    }
}
