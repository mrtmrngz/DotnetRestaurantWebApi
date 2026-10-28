using System.Net;
using System.Net.Http.Headers;
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

public class CreateCashCheckoutForTablesTests: BaseIntegrationTest
{
    private const string IdempotencyHeaderName = "X-Idempotency-Key";

    public CreateCashCheckoutForTablesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Route(Guid tableId) => $"/api/checkout/tables/{tableId}/cash";

    private static void SetIdempotencyKey(HttpClient client, string idempotencyKey)
    {
        client.DefaultRequestHeaders.Remove(IdempotencyHeaderName);
        client.DefaultRequestHeaders.Add(IdempotencyHeaderName, idempotencyKey);
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenRequestIsValid_ShouldCreateConfirmedAndPaidOrder()
    {
        // Arrange
        var table = await SeedTableAsync();
        var seededCart = await SeedCartWithItemsAsync(table.Id, price: 600m, quantity: 1);

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-api-idempotency-key-1");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Masa aktif, sepet dolu ve yetki uygun olduğu için nakit sipariş oluşturulmalıydı.");

        var responseBody = await response.ReadContentAsAsync<BaseResponse>();

        responseBody.Should().NotBeNull("Başarılı API isteğinde response body null gelmemeliydi.");
        responseBody!.Code.Should().Be(Codes.CHECKOUT_SUCCESS);
        responseBody.Message.Should().NotBeNullOrWhiteSpace();

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .Include(o => o.OrderItems)
            .SingleAsync();

        order.OrderStatus.Should().Be(OrderStatus.Confirmed, "nakit sipariş doğrudan onaylanır.");
        order.PaymentStatus.Should().Be(PaymentStatus.Paid, "nakit ödeme masada alındığı için ödenmiş kabul edilir.");
        order.RestaurantTableId.Should().Be(table.Id);
        order.UserId.Should().BeNull("masa siparişi kullanıcıya bağlanmaz.");
        order.IdempotencyKey.Should().Be("cash-api-idempotency-key-1");

        order.SubTotal.Should().Be(600m);
        order.ShippingCost.Should().Be(0m, "masa siparişlerinde kargo alınmaz.");
        order.Total.Should().Be(600m);
        order.AddressSnapshot.Should().BeNull("masa siparişinde adres snapshot'ı tutulmaz.");
        order.CheckoutSessionId.Should().BeNull("nakit ödemede Stripe oturumu açılmaz.");

        var orderItem = order.OrderItems.Should().ContainSingle().Subject;

        orderItem.ProductId.Should().Be(seededCart.ProductId);
        orderItem.ProductName.Should().Be("Test Ürün");
        orderItem.UnitPrice.Should().Be(600m);
        orderItem.Quantity.Should().Be(1);
        orderItem.TotalPrice.Should().Be(600m);
        orderItem.ProductSnapshot.Should().NotBeNullOrWhiteSpace("ürün snapshot'ı sipariş kalemine yazılmalıdır.");

        (await context.CartItems.CountAsync()).Should().Be(0, "sipariş sonrası masa sepeti boşaltılmalıdır.");
        (await context.Carts.CountAsync()).Should().Be(1, "sepet kaydı silinmemelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenIdempotencyKeyAlreadyUsed_ShouldReturn409()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedCartWithItemsAsync(table.Id);

        const string idempotencyKey = "cash-already-used-key";

        await SeedOrderWithIdempotencyKeyAsync(table.Id, idempotencyKey);

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, idempotencyKey);

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Aynı idempotency key ile sipariş daha önce başlatıldığı için 409 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Code.Should().Be(Codes.CONFLICT);
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenTableNotExist_ShouldReturn404()
    {
        // Arrange
        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-table-not-exist-key");

        // Act
        var response = await Client.PostAsync(Route(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Masa veritabanında bulunmadığı için 404 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Message.Should().Be("Masa bulunamadı.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenTableIsPassive_ShouldReturn404()
    {
        // Arrange
        var passiveTable = await SeedTableAsync(isActive: false);
        await SeedCartWithItemsAsync(passiveTable.Id);

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-passive-table-key");

        // Act
        var response = await Client.PostAsync(Route(passiveTable.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Pasif masa sipariş alabilen masa olarak kabul edilmediği için 404 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Message.Should().Be("Masa bulunamadı.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenCartNotExist_ShouldReturn404()
    {
        // Arrange
        var table = await SeedTableAsync();

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-no-cart-key");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Masanın henüz sepeti bulunmadığı için 404 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Message.Should().Be("Sepet bulunamadı.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenCartIsEmpty_ShouldReturn422()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedEmptyCartAsync(table.Id);

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-empty-cart-key");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Boş sepetle sipariş oluşturulamayacağı için 422 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        responseBody.Message.Should().Be("Sepetinizde sipariş oluşturulacak ürün bulunmuyor.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenCartContainsDeletedProduct_ShouldReturn409()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedCartWithItemsAsync(table.Id, productDeleted: true);

        var adminSetup = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-deleted-product-key");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Sepette satışta olmayan ürün bulunduğu için 409 dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody!.Message.Should().Be("Sepette artık satışta olmayan ürünler var.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenUserIsNotAuthenticated_ShouldReturn401()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedCartWithItemsAsync(table.Id);

        Client.DefaultRequestHeaders.Authorization = null;
        SetIdempotencyKey(Client, "cash-unauthorized-key");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 Unauthorized dönmeliydi.");
    }

    [Fact]
    public async Task CreateCashCheckoutForTables_WhenUserHasNoCreateCheckoutPermission_ShouldReturn403()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedCartWithItemsAsync(table.Id);

        var vanillaSetup = await CreateVanillaUserAsync();
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", vanillaSetup.AccessToken);
        SetIdempotencyKey(Client, "cash-forbidden-key");

        // Act
        var response = await Client.PostAsync(Route(table.Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının checkout:create izni olmadığı için 403 Forbidden dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(bool isActive = true)
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

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    private async Task<Guid> SeedEmptyCartAsync(Guid tableId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId
        };

        context.Carts.Add(cart);

        await context.SaveChangesAsync();

        return cart.Id;
    }

    private async Task<(Guid CartId, Guid ProductId)> SeedCartWithItemsAsync(Guid tableId, decimal price = 100m,
        int quantity = 1, bool productDeleted = false)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = CreateProduct(price, productDeleted);

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Test Kategori",
            Slug = $"test-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId
        };

        context.Carts.Add(cart);

        context.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = quantity
        });

        await context.SaveChangesAsync();

        return (cart.Id, product.Id);
    }

    private async Task<Guid> SeedOrderWithIdempotencyKeyAsync(Guid tableId, string idempotencyKey,
        decimal price = 100m)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = CreateProduct(price, isDeleted: false);

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Test Kategori",
            Slug = $"test-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

        // sipariş kalemi ürün FK'sı için gerçek ürün gerekir, sepet kaydı veritabanına yazılmaz
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId
        };

        cart.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = 1
        });

        var order = Domain.Entities.Order.CreateFromCart(cart, null, tableId, idempotencyKey, DateTime.UtcNow,
            OrderStatus.Confirmed);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    private static Domain.Entities.Product CreateProduct(decimal price, bool isDeleted)
    {
        return new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Test Ürün",
            Description = "Test ürün açıklaması",
            Slug = $"test-urun-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = isDeleted
        };
    }
}
