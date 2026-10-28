using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Checkout;

public class CreateCheckoutForTablesTests: BaseIntegrationTest
{
    private const string IdempotencyHeaderName = "X-Idempotency-Key";

    public CreateCheckoutForTablesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Route(Guid tableId) => $"/api/checkout/tables/{tableId}/online";

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCheckoutForTables_WhenRequestIsValid_ShouldReturn200AndCreateCheckoutSession()
    {
        // Arrange
        var table = await SeedTableWithCartAsync(price: 600m, quantity: 1);

        var adminSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "table-api-idempotency-key-1");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Masa, masa sepeti ve yetki uygun olduğu için ödeme oturumu açılmalıydı.");

        var responseBody = await response.ReadContentAsAsync<CheckoutResponse>();

        responseBody.Should().NotBeNull("Başarılı API isteğinde response body null gelmemeliydi.");
        responseBody!.Code.Should().Be(Codes.CHECKOUT_SESSION_CREATED);
        responseBody.SessionUrl.Should().Be(FakePaymentService.SessionUrl,
            "Ödeme sayfası adresi, açılan ödeme oturumundan dönmelidir.");

        // sipariş masaya bağlı olarak kaydedilmiş, sepet kalemleri temizlenmiş olmalı
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .Include(o => o.OrderItems)
            .SingleAsync();

        order.RestaurantTableId.Should().Be(table.Id);
        order.UserId.Should().BeNull("masa siparişi kullanıcıya bağlanmaz.");
        order.CheckoutSessionId.Should().Be(FakePaymentService.SessionId);
        order.CheckoutSessionUrl.Should().Be(FakePaymentService.SessionUrl);
        order.AddressSnapshot.Should().BeNull("masa siparişinde adres snapshot'ı tutulmaz.");
        order.ShippingCost.Should().Be(0m, "masa siparişlerinde kargo alınmaz.");

        order.OrderItems.Should().ContainSingle().Subject.TotalPrice.Should().Be(600m);

        (await context.CartItems.CountAsync()).Should().Be(0, "sipariş sonrası masa sepeti temizlenmelidir.");
        (await context.Carts.CountAsync()).Should().Be(1, "sepet kaydı silinmemelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateCheckoutForTables_WhenUserIsNotAuthenticated_ShouldReturn401()
    {
        // Arrange
        var table = await SeedTableWithCartAsync();

        Client.DefaultRequestHeaders.Authorization = null;
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "table-api-idempotency-key-401");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 Unauthorized dönmeliydi.");
    }

    [Fact]
    public async Task CreateCheckoutForTables_WhenUserHasNoCreateCheckoutPermission_ShouldReturn403()
    {
        // Arrange
        var table = await SeedTableWithCartAsync();

        var vanillaSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", vanillaSetup.AccessToken);
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "table-api-idempotency-key-403");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının checkout:create izni olmadığı için 403 Forbidden dönmeliydi.");
    }

    [Fact]
    public async Task CreateCheckoutForTables_WhenTableNotExist_ShouldReturn404()
    {
        // Arrange
        var notExistingTableId = Guid.NewGuid();

        var adminSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "table-api-idempotency-key-404");

        // Act
        var response = await Client.PostAsync(Route(notExistingTableId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Belirtilen masa veritabanında bulunmadığı için 404 Not Found dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody.Should().NotBeNull("Hata yanıtında body null gelmemeliydi.");
        responseBody!.Message.Should().Be("Masa bulunamadı.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Domain.Entities.RestaurantTable> SeedTableWithCartAsync(decimal price = 100m,
        int quantity = 1, bool isActive = true)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = Random.Shared.Next(1, 10_000),
            Capacity = 6,
            IsActive = isActive
        };

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Kategori",
            Slug = $"test-kategori-{Guid.NewGuid()}"
        };

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Test Ürün",
            Description = "Test ürün açıklaması",
            Slug = $"test-urun-{Guid.NewGuid()}",
            Price = price,
            CategoryId = category.Id
        };

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = table.Id
        };

        context.RestaurantTables.Add(table);
        context.Categories.Add(category);
        context.Products.Add(product);
        context.Carts.Add(cart);

        context.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = quantity
        });

        await context.SaveChangesAsync();

        return table;
    }
}
