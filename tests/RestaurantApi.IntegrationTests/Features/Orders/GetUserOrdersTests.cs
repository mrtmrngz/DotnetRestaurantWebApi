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
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Infrastructure.Settings;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class GetUserOrdersTests : BaseIntegrationTest
{
    private const string Route = "/api/orders/users";

    public GetUserOrdersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetUserOrders_WhenUserHasOrders_ShouldReturnOrdersNewestFirst()
    {
        // Arrange: aynı kullanıcının iki siparişi (biri daha eski)
        var customer = await CreateVanillaUserAsync();

        var olderOrderId = await SeedPaidOrderAsync(customer.User.Id, OrderStatus.Delivered, 320m,
            DateTime.UtcNow.AddDays(-2));
        var newerOrderId = await SeedPaidOrderAsync(customer.User.Id, OrderStatus.Confirmed, 450m,
            DateTime.UtcNow.AddHours(-3));

        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

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
        body.Data[0].SubTotal.Should().Be(450m);
        body.Data[0].Total.Should().BeGreaterThan(0m);
        body.Data[0].OrderDate.Should().BeCloseTo(DateTime.UtcNow.AddHours(-3), TimeSpan.FromMinutes(1));

        body.Data[1].OrderStatus.Should().Be(OrderStatus.Delivered);
        body.Data[1].SubTotal.Should().Be(320m);
    }

    [Fact]
    public async Task GetUserOrders_WhenUserHasNoOrders_ShouldReturnEmptyList()
    {
        // Arrange: kullanıcı var ama hiç siparişi yok
        var customer = await CreateVanillaUserAsync();
        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "siparişi olmayan kullanıcı için hata değil, boş liste döner.");
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().NotBeNull();
        body.Data.Should().BeEmpty("kullanıcının siparişi bulunmadığı için liste boş olmalıdır.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetUserOrders_WhenTokenIsMissing_ShouldReturn401()
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
    public async Task GetUserOrders_WhenTokenHasNoUserIdClaim_ShouldReturn403()
    {
        SetBearerToken(CreateToken(userId: null));

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "SameUser policy kullanıcıyı tanımlayamadığı için isteği reddetmelidir.");
    }

    [Fact]
    public async Task GetUserOrders_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange: token geçerli ama kullanıcı kaydı bulunmuyor (ör. kullanıcı silinmiş)
        SetBearerToken(CreateToken(userId: Guid.NewGuid()));

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Kullanıcı bulunamadı.");
    }

    #endregion


    #region SETUP

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetUserOrdersQueryResult>>>();
    }

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

    private async Task<Guid> SeedPaidOrderAsync(Guid userId, OrderStatus orderStatus, decimal price,
        DateTime orderDate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Liste Test Ürün",
            Description = "Liste testleri için ürün",
            Slug = $"liste-test-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Liste Test Kategori",
            Slug = $"liste-kategori-{Guid.NewGuid()}"
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
            orderDate, orderStatus);

        order.MarkPaid();

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    #endregion
}
