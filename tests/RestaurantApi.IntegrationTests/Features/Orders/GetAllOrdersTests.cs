using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Orders.Queries.GetAllOrdersQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class GetAllOrdersTests : BaseIntegrationTest
{
    private const string Route = "/api/orders/panel";

    public GetAllOrdersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetAllOrders_WhenNoFilters_ShouldReturnAllOrdersNewestFirst()
    {
        // Arrange: bir masa siparişi (daha eski, önce eklenir) + bir kullanıcı siparişi (daha yeni)
        var table = await SeedTableAsync(isActive: true);
        var tableOrderId = await SeedTableOrderAsync(table.Id, PaymentStatus.Paid, OrderStatus.Delivered,
            600m, DateTime.UtcNow.AddDays(-1));

        var user = await CreateVanillaUserAsync();
        var userOrderId = await SeedUserOrderAsync(user.User.Id, PaymentStatus.Paid, OrderStatus.Confirmed,
            450m, DateTime.UtcNow.AddHours(-1));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().HaveCount(2, "panel listesi kullanıcı ve masa siparişlerini birlikte döner.");

        // en yeni sipariş ilk sırada gelir (OrderDate azalan; ekleme sırasının tersi)
        body.Data[0].OrderId.Should().Be(userOrderId);
        body.Data[1].OrderId.Should().Be(tableOrderId);

        // kullanıcı siparişi satırı: sahip bilgisi kullanıcıya işaret eder
        var userRow = body.Data[0];
        userRow.Type.Should().Be(GetAllOrdersQueryResult.OwnerUser);
        userRow.UserId.Should().Be(user.User.Id);
        userRow.TableId.Should().BeNull();
        userRow.OrderStatus.Should().Be(OrderStatus.Confirmed);
        userRow.SubTotal.Should().Be(450m);
        userRow.Total.Should().BeGreaterThan(0m);

        // masa siparişi satırı: sahip bilgisi masaya işaret eder, kullanıcı alanı boştur
        var tableRow = body.Data[1];
        tableRow.Type.Should().Be(GetAllOrdersQueryResult.OwnerTable);
        tableRow.TableId.Should().Be(table.Id);
        tableRow.UserId.Should().BeNull();
        tableRow.OrderStatus.Should().Be(OrderStatus.Delivered);
        tableRow.SubTotal.Should().Be(600m);
    }

    [Fact]
    public async Task GetAllOrders_WhenTypeFilterIsTable_ShouldReturnOnlyTableOrders()
    {
        // Arrange: hem kullanıcı hem masa siparişi var; filtre sadece masayı getirmeli
        var user = await CreateVanillaUserAsync();
        await SeedUserOrderAsync(user.User.Id, PaymentStatus.Paid, OrderStatus.Confirmed, 450m,
            DateTime.UtcNow.AddHours(-1));

        var table = await SeedTableAsync(isActive: true);
        var tableOrderId = await SeedTableOrderAsync(table.Id, PaymentStatus.Paid, OrderStatus.Delivered,
            600m, DateTime.UtcNow.AddHours(-2));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}?type={GetAllOrdersQuery.TypeTable}");

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        var row = body.Data.Should().ContainSingle("type=table filtresi kullanıcı siparişlerini dışlar.").Subject;
        row.OrderId.Should().Be(tableOrderId);
        row.Type.Should().Be(GetAllOrdersQueryResult.OwnerTable);
        row.TableId.Should().Be(table.Id);
        row.UserId.Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrders_WhenTypeIsInvalid_ShouldReturnEmptyList()
    {
        // Arrange: sipariş var ama type filtresi desteklenmeyen bir değer taşıyor
        var table = await SeedTableAsync(isActive: true);
        await SeedTableOrderAsync(table.Id, PaymentStatus.Paid, OrderStatus.Delivered, 600m,
            DateTime.UtcNow.AddHours(-1));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}?type=owner");

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "geçersiz filtre değeri hata değil, boş liste ile karşılanır.");
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().BeEmpty("desteklenmeyen bir type filtresi için sonuç bulunamaz.");
    }

    [Fact]
    public async Task GetAllOrders_WhenOrderStatusIsInvalid_ShouldReturnEmptyList()
    {
        // Arrange: siparişler var ama sipariş durumu filtresi enum ile eşleşmiyor
        var table = await SeedTableAsync(isActive: true);
        await SeedTableOrderAsync(table.Id, PaymentStatus.Paid, OrderStatus.Delivered, 600m,
            DateTime.UtcNow.AddHours(-1));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}?order-status=Kargoda");

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "geçersiz filtre değeri hata değil, boş liste ile karşılanır.");
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().BeEmpty("geçersiz sipariş durumu filtresi için sonuç bulunamaz.");
    }

    [Fact]
    public async Task GetAllOrders_WhenPaymentStatusIsInvalid_ShouldReturnEmptyList()
    {
        // Arrange: siparişler var ama ödeme durumu filtresi enum ile eşleşmiyor
        var table = await SeedTableAsync(isActive: true);
        await SeedTableOrderAsync(table.Id, PaymentStatus.Paid, OrderStatus.Delivered, 600m,
            DateTime.UtcNow.AddHours(-1));

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}?payment-status=Odenmedi");

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "geçersiz filtre değeri hata değil, boş liste ile karşılanır.");
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().BeEmpty("geçersiz ödeme durumu filtresi için sonuç bulunamaz.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetAllOrders_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetAllOrders_WhenUserHasNoReadOrderPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> orders:read izni yok
        var vanilla = await CreateVanillaUserAsync();
        await SeedUserOrderAsync(vanilla.User.Id, PaymentStatus.Paid, OrderStatus.Confirmed, 450m,
            DateTime.UtcNow.AddHours(-1));

        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının orders:read izni bulunmadığı için 403 dönmelidir.");
    }

    #endregion

    #region SETUP

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetAllOrdersQueryResult>>>();
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

    // Kullanıcıya bağlı sipariş oluşturur; ödeme durumu verilen değere göre işaretlenir.
    private async Task<Guid> SeedUserOrderAsync(Guid userId, PaymentStatus paymentStatus, OrderStatus orderStatus,
        decimal price, DateTime orderDate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = AddProduct(context, price);

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

        // kullanıcı siparişi: kullanıcı var, masa yok
        var order = Domain.Entities.Order.CreateFromCart(cart, userId, null, $"key-{Guid.NewGuid()}",
            orderDate, orderStatus);

        if (paymentStatus == PaymentStatus.Paid)
        {
            order.MarkPaid();
        }

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }


    // Masaya bağlı sipariş oluşturur; ödeme durumu verilen değere göre işaretlenir.
    private async Task<Guid> SeedTableOrderAsync(Guid tableId, PaymentStatus paymentStatus, OrderStatus orderStatus,
        decimal price, DateTime orderDate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = AddProduct(context, price);

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

        if (paymentStatus == PaymentStatus.Paid)
        {
            order.MarkPaid();
        }

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    private static Domain.Entities.Product AddProduct(ApiContext context, decimal price)
    {
        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Panel Liste Test Ürün",
            Description = "Panel listesi testleri için ürün",
            Slug = $"panel-liste-test-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Panel Liste Test Kategori",
            Slug = $"panel-liste-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

        return product;
    }


    #endregion
}
