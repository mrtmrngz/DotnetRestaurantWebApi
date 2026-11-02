using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class GetPanelOrderDetailTests : BaseIntegrationTest
{
    private const string OrderNotFoundMessage = "Sipariş bulunamadı.";

    public GetPanelOrderDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetPanelOrderDetail_WhenOrderBelongsToUser_ShouldReturnUserOrderDetail()
    {
        var customer = await CreateVanillaUserAsync(user =>
        {
            user.Name = "Ayşe";
            user.Surname = "Yılmaz";
        });

        var orderId = await SeedUserOrderAsync(customer.User.Id, withAddress: true);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.OrderId.Should().Be(orderId);

        // kullanıcı siparişi: müşteri tam adı ve adres anlık görüntüsü dolu, masa numarası boş
        body.Data.CustomerFullName.Should().Be("Ayşe Yılmaz");
        body.Data.TableNumber.Should().BeNull("kullanıcı siparişinde masa bulunmaz.");
        body.Data.AddressSnapshot.Should().NotBeNullOrWhiteSpace()
            .And.Contain("RecipientName", "adres anlık görüntüsü sipariş anındaki adresi taşımalıdır.");

        // özet alanları siparişten eşlenir (kargo, 500 ve üzeri siparişlerde ücretsizdir)
        body.Data.SubTotal.Should().Be(910m);
        body.Data.ShippingCost.Should().Be(0m);
        body.Data.Total.Should().Be(910m);
        body.Data.OrderStatus.Should().Be(OrderStatus.Confirmed);
        body.Data.PaymentStatus.Should().Be(PaymentStatus.Paid);
        body.Data.OrderDate.Should().BeCloseTo(DateTime.UtcNow.AddHours(-2), TimeSpan.FromMinutes(1));

        // kalemler ürün adı, birim fiyat ve adet ile döner
        body.Data.OrderItems.Should().HaveCount(2);
        body.Data.OrderItems.Select(x => x.ProductName)
            .Should().BeEquivalentTo(new[] { "Panel Test Ürün", "Panel Test İkinci Ürün" });
        body.Data.OrderItems.Select(x => x.UnitPrice).Should().BeEquivalentTo(new[] { 600m, 155m });
        body.Data.OrderItems.Select(x => x.Quantity).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetPanelOrderDetail_WhenOrderBelongsToTable_ShouldReturnTableOrderDetail()
    {
        // Arrange: masa siparişi
        var (tableNumber, orderId) = await SeedTableOrderAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data.OrderId.Should().Be(orderId);

        // masa siparişi: masa numarası dolu, müşteri adı ve adres boş
        body.Data.TableNumber.Should().Be(tableNumber);
        body.Data.CustomerFullName.Should().BeNull("masa siparişinin müşterisi olmadığı için ad boş döner.");
        body.Data.AddressSnapshot.Should().BeNullOrEmpty("masa siparişlerinde adres tutulmaz.");

        // masa siparişlerinde kargo alınmaz
        body.Data.ShippingCost.Should().Be(0m);
        body.Data.Total.Should().Be(400m);
        body.Data.OrderStatus.Should().Be(OrderStatus.Delivered);
        body.Data.PaymentStatus.Should().Be(PaymentStatus.Paid);
        body.Data.OrderItems.Should().ContainSingle().Which.ProductName.Should().Be("Panel Test Ürün");
    }

    [Fact]
    public async Task GetPanelOrderDetail_WhenUserIsDeleted_ShouldNotReturnCustomerName()
    {
        // Arrange: sipariş sahibi kullanıcı silinmiş (IsDeleted = true)
        var deletedCustomer = await CreateVanillaUserAsync(user =>
        {
            user.Name = "Silinmis";
            user.Surname = "Kullanici";
            user.IsDeleted = true;
        });

        var orderId = await SeedUserOrderAsync(deletedCustomer.User.Id, withAddress: true);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "sipariş yine döner; yalnızca silinmiş kullanıcının adı gizlenir.");
        body!.Data.OrderId.Should().Be(orderId);
        body.Data.CustomerFullName.Should().BeNull("silinmiş kullanıcının adı panelde gösterilmemelidir.");

        // siparişin geri kalanı silinen kullanıcıdan bağımsız olarak dönmeye devam eder
        body.Data.OrderItems.Should().HaveCount(2);
        body.Data.AddressSnapshot.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetPanelOrderDetail_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        var (_, orderId) = await SeedTableOrderAsync();

        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetPanelOrderDetail_WhenUserHasNoReadOrderPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> orders:read izni yok
        var (_, orderId) = await SeedTableOrderAsync();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının orders:read izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task GetPanelOrderDetail_WhenOrderDoesNotExist_ShouldReturn404()
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
        body.Message.Should().Be(OrderNotFoundMessage);
    }

    #endregion

    #region SETUP

    private static string Route(Guid orderId) => $"/api/orders/panel/{orderId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetPanelOrderDetailQueryResult>>();
    }

    // Tek kalemli (400 x1 -> ara toplam 400, kargo 0) masa siparişi oluşturur.
    private async Task<(int TableNumber, Guid OrderId)> SeedTableOrderAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = 42,
            Capacity = 4,
            IsActive = true
        };

        context.RestaurantTables.Add(table);

        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), RestaurantTableId = table.Id };
        AddCartItem(context, cart, "Panel Test Ürün", 400m, 1);

        // masa siparişi: kullanıcı yok, masa var
        var order = Domain.Entities.Order.CreateFromCart(cart, null, table.Id, $"key-{Guid.NewGuid()}",
            DateTime.UtcNow.AddHours(-1), OrderStatus.Delivered);

        order.MarkPaid();

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return (table.Number, order.Id);
    }

    // İki kalemli (600 x1, 155 x2 -> ara toplam 910, kargo 0) kullanıcı siparişi oluşturur.
    private async Task<Guid> SeedUserOrderAsync(Guid userId, bool withAddress)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };

        AddCartItem(context, cart, "Panel Test Ürün", 600m, 1);
        AddCartItem(context, cart, "Panel Test İkinci Ürün", 155m, 2);

        // kullanıcı siparişi: kullanıcı var, masa yok
        var order = Domain.Entities.Order.CreateFromCart(cart, userId, null, $"key-{Guid.NewGuid()}",
            DateTime.UtcNow.AddHours(-2), OrderStatus.Confirmed);

        if (withAddress)
        {
            var address = new Domain.Entities.Address
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Ev",
                RecipientName = "Ayşe Yılmaz",
                City = "İstanbul",
                Town = "Kadıköy",
                Neighborhood = "Caferağa",
                Street = "Moda",
                BuildingInfo = "A Blok",
                BuildingNumber = "12",
                PhoneNumber = "05321112233",
                ZipCode = "34710"
            };

            context.Addresses.Add(address);

            // sipariş anındaki adres JSON olarak saklanır
            order.HandleSetAddress(address);
        }

        order.MarkPaid();

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    private static void AddCartItem(ApiContext context, Domain.Entities.Cart cart, string title, decimal price,
        int quantity)
    {
        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Panel testleri için ürün",
            Slug = $"panel-test-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Panel Test Kategori",
            Slug = $"panel-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

        cart.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = quantity
        });
    }

    #endregion
}
