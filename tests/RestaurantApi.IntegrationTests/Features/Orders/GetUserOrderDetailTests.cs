using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Infrastructure.Settings;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class GetUserOrderDetailTests : BaseIntegrationTest
{
    private const string OrderNotFoundMessage = "Sipariş bulunamadı.";
    private const string UserNotFoundMessage = "Kullanıcı bulunamadı.";

    public GetUserOrderDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetUserOrderDetail_WhenOrderBelongsToUser_ShouldReturnOrderDetail()
    {
        // Arrange: iki kalemli (600 x1, 155 x2), adresli ve ödemesi alınmış bir kullanıcı siparişi
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderWithAddressAsync(customer.User.Id, OrderStatus.Confirmed);

        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.OrderId.Should().Be(orderId);

        body.Data.OrderStatus.Should().Be(OrderStatus.Confirmed);
        body.Data.PaymentStatus.Should().Be(PaymentStatus.Paid);
        body.Data.SubTotal.Should().Be(910m);
        body.Data.ShippingCost.Should().Be(0m);
        body.Data.Total.Should().Be(910m);

        body.Data.AddressSnapshot.Should().NotBeNullOrWhiteSpace()
            .And.Contain("RecipientName", "adres anlık görüntüsü sipariş anındaki adresi taşımalıdır.")
            .And.Contain("Moda");

        // sipariş kalemleri yalnızca istenen alanlarla (ürün adı, birim fiyat, adet) döner
        body.Data.OrderItems.Should().HaveCount(2);
        body.Data.OrderItems.Select(x => x.ProductName)
            .Should().BeEquivalentTo(new[] { "Detay Test Ürün", "Detay Test İkinci Ürün" });
        body.Data.OrderItems.Select(x => x.UnitPrice).Should().BeEquivalentTo(new[] { 600m, 155m });
        body.Data.OrderItems.Select(x => x.Quantity).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetUserOrderDetail_WhenOrderIdIsNotGuid_ShouldReturn400()
    {
        // Arrange: sipariş id'si geçerli bir GUID değil
        var customer = await CreateVanillaUserAsync();
        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.GetAsync("/api/orders/users/siparis-yok");

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "geçersiz sipariş id'si doğrulama hatası olarak 400 dönmelidir.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR);
        body.Errors.Should().ContainSingle().Which.Field.Should().Be("OrderId");
        body.Errors[0].Message.Should().Be("Sipariş ID bilgisi zorunludur ve boş olamaz.");
    }

    [Fact]
    public async Task GetUserOrderDetail_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderWithAddressAsync(customer.User.Id, OrderStatus.Confirmed);

        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetUserOrderDetail_WhenTokenHasNoUserIdClaim_ShouldReturn403()
    {
        // Arrange: token geçerli ama içinde kullanıcı kimliği yok -> SameUser policy kullanıcıyı çözemez
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderWithAddressAsync(customer.User.Id, OrderStatus.Confirmed);

        SetBearerToken(CreateToken(userId: null));

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "SameUser policy kullanıcıyı tanımlayamadığı için isteği reddetmelidir.");
    }

    [Fact]
    public async Task GetUserOrderDetail_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange: token geçerli ama kullanıcı kaydı bulunmuyor (ör. kullanıcı silinmiş)
        SetBearerToken(CreateToken(Guid.NewGuid()));

        // Act
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be(UserNotFoundMessage);
    }

    [Fact]
    public async Task GetUserOrderDetail_WhenOrderDoesNotExist_ShouldReturn404()
    {
        // Arrange: kullanıcı var ama sipariş kaydı yok
        var customer = await CreateVanillaUserAsync();
        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be(OrderNotFoundMessage);
    }

    [Fact]
    public async Task GetUserOrderDetail_WhenOrderBelongsToAnotherUser_ShouldReturn404()
    {
        // Arrange: sipariş var ve başka bir kullanıcıya ait (IDOR denemesi)
        var owner = await CreateVanillaUserAsync();
        var orderId = await SeedOrderWithAddressAsync(owner.User.Id, OrderStatus.Confirmed);

        var anotherUser = await CreateVanillaUserAsync();
        SetBearerToken(anotherUser.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(orderId));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "başkasına ait sipariş, sahibi olmayan kullanıcıya açılmamalıdır.");
        body!.Code.Should().Be(Codes.NOT_FOUND);

        // siparişin varlığı sızdırılmaz: "sipariş yok" ile aynı mesaj döner
        body.Message.Should().Be(OrderNotFoundMessage,
            "başka bir kullanıcıya ait siparişin varlığı istemciye sızdırılmamalıdır.");
    }

    #endregion

    #region SETUP

    private static string Route(Guid orderId) => $"/api/orders/users/{orderId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetUserOrderDetailQueryResult>>();
    }

    // Kullanıcı kimliği claim'i olmayan token üretmek için userId null verilebilir (SameUser policy 403 döner).
    private string CreateToken(Guid? userId)
    {
        using var scope = Factory.Services.CreateScope();

        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        var claims = new List<Claim> { new(ClaimTypes.Role, "USER") };

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // İki kalemli (600 x1, 155 x2 -> ara toplam 910, kargo 0), adresli ve ödemesi alınmış sipariş oluşturur.
    private async Task<Guid> SeedOrderWithAddressAsync(Guid userId, OrderStatus orderStatus)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };

        AddCartItem(context, cart, "Detay Test Ürün", 600m, 1);
        AddCartItem(context, cart, "Detay Test İkinci Ürün", 155m, 2);

        // kullanıcı siparişi: kullanıcı var, masa yok
        var order = Domain.Entities.Order.CreateFromCart(cart, userId, null, $"key-{Guid.NewGuid()}",
            DateTime.UtcNow.AddHours(-1), orderStatus);

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
            Description = "Detay testleri için ürün",
            Slug = $"detay-test-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Detay Test Kategori",
            Slug = $"detay-kategori-{Guid.NewGuid()}"
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
