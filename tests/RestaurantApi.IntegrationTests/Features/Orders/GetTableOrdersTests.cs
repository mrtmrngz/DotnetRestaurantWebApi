using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class GetTableOrdersTests : BaseIntegrationTest
{
    public GetTableOrdersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetTableOrders_WhenTableHasOrders_ShouldReturnOrdersNewestFirst()
    {
        // Arrange: aynı masanın iki siparişi (biri daha eski)
        var table = await SeedTableAsync(isActive: true);

        var olderOrderId = await SeedTableOrderAsync(table.Id, OrderStatus.Delivered, 250m,
            DateTime.UtcNow.AddDays(-1));
        var newerOrderId = await SeedTableOrderAsync(table.Id, OrderStatus.Confirmed, 600m,
            DateTime.UtcNow.AddHours(-2));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(table.Id));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().HaveCount(2);

        // en yeni sipariş ilk sırada gelir
        body.Data[0].OrderId.Should().Be(newerOrderId);
        body.Data[1].OrderId.Should().Be(olderOrderId);

        // liste satırındaki alanlar siparişten doğru eşlenir
        body.Data[0].OrderStatus.Should().Be(OrderStatus.Confirmed);
        body.Data[0].SubTotal.Should().Be(600m);
        body.Data[0].Total.Should().BeGreaterThan(0m);
        body.Data[0].OrderDate.Should().BeCloseTo(DateTime.UtcNow.AddHours(-2), TimeSpan.FromMinutes(1));

        body.Data[1].OrderStatus.Should().Be(OrderStatus.Delivered);
        body.Data[1].SubTotal.Should().Be(250m);
    }

    [Fact]
    public async Task GetTableOrders_WhenTableHasNoOrders_ShouldReturnEmptyList()
    {
        // Arrange: masa var ama hiç siparişi yok
        var table = await SeedTableAsync(isActive: true);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(table.Id));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "siparişi olmayan masa için hata değil, boş liste döner.");
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().NotBeNull();
        body.Data.Should().BeEmpty("masanın siparişi bulunmadığı için liste boş olmalıdır.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetTableOrders_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        var table = await SeedTableAsync(isActive: true);
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route(table.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetTableOrders_WhenUserHasNoReadOrderPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> orders:read izni yok
        var table = await SeedTableAsync(isActive: true);
        await SeedTableOrderAsync(table.Id, OrderStatus.Confirmed, 600m, DateTime.UtcNow.AddHours(-1));

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(table.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının orders:read izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task GetTableOrders_WhenTableDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Masa bulunamadı.");
    }

    #endregion


    #region SETUP

    private static string Route(Guid tableId) => $"/api/orders/tables/{tableId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetTableOrdersQueryResult>>>();
    }

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(bool isActive)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = Random.Shared.Next(1, 10_000),
            Capacity = 4,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    // Masaya bağlı, ödemesi alınmış sipariş oluşturur; sipariş tarihi sıralama testi için verilebilir.
    private async Task<Guid> SeedTableOrderAsync(Guid tableId, OrderStatus orderStatus, decimal price,
        DateTime orderDate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Masa Liste Test Ürün",
            Description = "Masa listesi testleri için ürün",
            Slug = $"masa-liste-test-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Masa Liste Test Kategori",
            Slug = $"masa-liste-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

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

        // masa siparişi: kullanıcı yok, masa var (kargo ve adres tutulmaz)
        var order = Domain.Entities.Order.CreateFromCart(cart, null, tableId, $"key-{Guid.NewGuid()}",
            orderDate, orderStatus);

        order.MarkPaid();

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    #endregion


}
