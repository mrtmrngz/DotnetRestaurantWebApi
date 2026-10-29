using System.Net;
using System.Net.Http.Headers;
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

namespace RestaurantApi.IntegrationTests.Features.Orders;

// PATCH api/orders/users/status/{orderId} ucunun uçtan uca davranışı.
// Handler doğrudan çağrılmaz; istekler gerçek HTTP ile gönderilir, sonuçlar veritabanından doğrulanır.
// Kapsam: yetkilendirme (401/403), doğrulama (400), sipariş bulma (404), sahiplik (403),
// durum çakışması (409), ödeme ön koşulu (422) ve başarılı güncelleme (200).
public class OrderStatusUpdateTests : BaseIntegrationTest
{
    public OrderStatusUpdateTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task OrderStatusUpdate_WhenRequestIsValid_ShouldUpdateOrderStatus()
    {
        // Arrange: ödemesi alınmış, durumu Pending olan kullanıcı siparişi
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedPaidUserOrderAsync(customer.User.Id, OrderStatus.Pending);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: panel yetkilisi siparişi Confirmed'a ilerletir
        var response = await PatchAsync(orderId, customer.User.Id, OrderStatus.Confirmed);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        body.Message.Should().Be("Sipariş durumu Confirmed olarak başarıyla güncellendi.");

        order.OrderStatus.Should().Be(OrderStatus.Confirmed);
        order.PaymentStatus.Should().Be(PaymentStatus.Paid, "ödeme durumu bu uçtan değişmemelidir.");
        order.UserId.Should().Be(customer.User.Id, "sipariş sahibi korunmalıdır.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task OrderStatusUpdate_WhenBodyIsEmpty_ShouldReturn400()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: gövde boş -> alan doğrulamaları devreye girer (orderId route'tan geldiği için geçerlidir)
        var response = await PatchRawAsync(Guid.NewGuid(), "{}");

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body!.Errors.Select(x => x.Message)
            .Should().Contain("Kullanıcı ID bilgisi zorunludur.")
            .And.Contain("Sipariş durumu zorunludur.");
    }

    [Fact]
    public async Task OrderStatusUpdate_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await PatchAsync(Guid.NewGuid(), Guid.NewGuid(), OrderStatus.Confirmed);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task OrderStatusUpdate_WhenUserHasNoUpdateOrderPermission_ShouldReturn403()
    {
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        var orderId = await SeedPaidUserOrderAsync(vanilla.User.Id, OrderStatus.Pending);

        // Act
        var response = await PatchAsync(orderId, vanilla.User.Id, OrderStatus.Confirmed);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının orders:update izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task OrderStatusUpdate_WhenOrderDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PatchAsync(Guid.NewGuid(), Guid.NewGuid(), OrderStatus.Confirmed);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Sipariş bulunamadı.");
    }


    [Fact]
    public async Task OrderStatusUpdate_WhenOrderBelongsToAnotherUser_ShouldReturn403()
    {
        // Arrange: sipariş bir müşteriye ait, istek gövdesinde başka bir kullanıcı id'si gönderiliyor
        var owner = await CreateVanillaUserAsync();
        var otherUser = await CreateVanillaUserAsync();
        var orderId = await SeedPaidUserOrderAsync(owner.User.Id, OrderStatus.Pending);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PatchAsync(orderId, otherUser.User.Id, OrderStatus.Confirmed);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "gövdedeki kullanıcı siparişin sahibi olmadığı için 403 dönmelidir.");
        body!.Code.Should().Be(Codes.FORBIDDEN);
        body.Message.Should().Be("Bu sipariş size ait değil.");
        order.OrderStatus.Should().Be(OrderStatus.Pending, "reddedilen istek hiçbir şeyi değiştirmemelidir.");
    }

    [Fact]
    public async Task OrderStatusUpdate_WhenNewStatusEqualsCurrentStatus_ShouldReturn409()
    {
        // Arrange: sipariş zaten Confirmed
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedPaidUserOrderAsync(customer.User.Id, OrderStatus.Confirmed);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PatchAsync(orderId, customer.User.Id, OrderStatus.Confirmed);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Sipariş zaten Confirmed durumunda.");
    }

    [Fact]
    public async Task OrderStatusUpdate_WhenOrderIsNotPaid_ShouldReturn422()
    {
        // Arrange: ödemesi henüz alınmamış sipariş (PaymentStatus = Pending)
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedUserOrderAsync(customer.User.Id, PaymentStatus.Pending, OrderStatus.Pending);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PatchAsync(orderId, customer.User.Id, OrderStatus.Confirmed);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "ödemesi tamamlanmayan siparişin durumu güncellenemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Ödemesi tamamlanmayan siparişin durumu güncellenemez.");
        order.OrderStatus.Should().Be(OrderStatus.Pending);
    }

    #endregion


    #region SETUP

    private sealed record OrderState(PaymentStatus PaymentStatus, OrderStatus OrderStatus, Guid? UserId);

    private static string Route(Guid orderId) => $"/api/orders/users/status/{orderId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> PatchAsync(Guid orderId, Guid userId, OrderStatus newStatus)
    {
        // enum, API ile aynı biçimde (string) gönderilir
        var payload = $$"""{"userId":"{{userId}}","newStatus":"{{newStatus}}"}""";

        return await PatchRawAsync(orderId, payload);
    }

    private async Task<HttpResponseMessage> PatchRawAsync(Guid orderId, string jsonBody)
    {
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, Route(orderId))
        {
            Content = content
        };

        return await Client.SendAsync(request);
    }

    private async Task<OrderState> GetOrderAsync(Guid orderId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .AsNoTracking()
            .FirstAsync(x => x.Id == orderId);

        return new OrderState(order.PaymentStatus, order.OrderStatus, order.UserId);
    }

    // Ödemesi alınmış (Paid) kullanıcı siparişi: Order.MarkPaid() yalnızca Pending iken çalışır.
    private Task<Guid> SeedPaidUserOrderAsync(Guid userId, OrderStatus orderStatus)
    {
        return SeedUserOrderAsync(userId, PaymentStatus.Paid, orderStatus);
    }

    // Kullanıcıya bağlı sipariş oluşturur (sipariş kalemi ürün FK'si için kategori + ürün gerekir).
    private async Task<Guid> SeedUserOrderAsync(Guid userId, PaymentStatus paymentStatus, OrderStatus orderStatus)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Sipariş Test Ürün",
            Description = "Sipariş durumu testleri için ürün",
            Slug = $"siparis-test-{Guid.NewGuid()}",
            Price = 150m,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Sipariş Test Kategori",
            Slug = $"siparis-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

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
            DateTime.UtcNow, orderStatus);

        if (paymentStatus == PaymentStatus.Paid)
        {
            order.MarkPaid();
        }

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    #endregion


}
