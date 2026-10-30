using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Infrastructure.Settings;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Orders;

public class OrderCancelTests : BaseIntegrationTest
{
    public OrderCancelTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task OrderCancel_WhenRequestIsValid_ShouldCancelOrderAndMarkPaymentRefunded()
    {
        // Arrange: ödemesi alınmış (Paid) ve hazırlanmakta olan kullanıcı siparişi
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderAsync(customer.User.Id, PaymentStatus.Paid, OrderStatus.Preparing);

        SetBearerToken(customer.AccessToken);

        // Act: kullanıcı kendi siparişini iptal eder
        var response = await Client.PatchAsync(Route(orderId), null);

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        body.Message.Should().Be("Sipariş iptal edildi ve ödeme durumu iade olarak işaretlendi.");

        order.OrderStatus.Should().Be(OrderStatus.Canceled);
        order.PaymentStatus.Should().Be(PaymentStatus.Refunded,
            "iptal ile birlikte ödeme durumu iade olarak işaretlenir.");
        order.UserId.Should().Be(customer.User.Id, "sipariş sahibi korunmalıdır.");
    }

    #endregion

    #region AUTHENTICATION AND AUTHORIZATION TESTS

    [Fact]
    public async Task OrderCancel_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PatchAsync(Route(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task OrderCancel_WhenTokenHasNoUserIdClaim_ShouldReturn403()
    {
        // Arrange: imzası geçerli ama kullanıcı id claim'i olmayan token -> SameUser policy reddeder
        SetBearerToken(CreateTokenWithoutUserIdClaim());

        // Act
        var response = await Client.PatchAsync(Route(Guid.NewGuid()), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "SameUser policy kullanıcıyı tanımlayamadığı için isteği reddetmelidir.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task OrderCancel_WhenOrderDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var customer = await CreateVanillaUserAsync();
        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.PatchAsync(Route(Guid.NewGuid()), null);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Sipariş bulunamadı.");
    }

    [Fact]
    public async Task OrderCancel_WhenOrderBelongsToAnotherUser_ShouldReturn404()
    {
        // Arrange: sipariş başka bir kullanıcıya ait; varlığı sızdırılmamalı
        var owner = await CreateVanillaUserAsync();
        var requester = await CreateVanillaUserAsync();
        var orderId = await SeedOrderAsync(owner.User.Id, PaymentStatus.Paid, OrderStatus.Preparing);

        SetBearerToken(requester.AccessToken);

        // Act
        var response = await Client.PatchAsync(Route(orderId), null);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "başka kullanıcıya ait siparişin varlığı 404 ile gizlenmelidir (403 değil).");
        body!.Message.Should().Be("Sipariş bulunamadı.");

        order.OrderStatus.Should().Be(OrderStatus.Preparing, "reddedilen istek hiçbir şeyi değiştirmemelidir.");
        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task OrderCancel_WhenOrderIsNotPaid_ShouldReturn422()
    {
        // Arrange: ödemesi henüz alınmamış sipariş
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderAsync(customer.User.Id, PaymentStatus.Pending, OrderStatus.Pending);

        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.PatchAsync(Route(orderId), null);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "ödemesi tamamlanmayan sipariş iptal edilemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Ödemesi tamamlanmayan siparişin durumu güncellenemez.");
        order.OrderStatus.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task OrderCancel_WhenOrderIsAlreadyCanceled_ShouldReturn409()
    {
        // Arrange: ödemesi alınmış ama daha önce iptal edilmiş sipariş
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderAsync(customer.User.Id, PaymentStatus.Paid, OrderStatus.Canceled);

        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.PatchAsync(Route(orderId), null);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Sipariş zaten iptal edilmiş.");
    }

    [Fact]
    public async Task OrderCancel_WhenOrderIsDelivered_ShouldReturn422()
    {
        // Arrange: teslim edilmiş sipariş iptal edilemez
        var customer = await CreateVanillaUserAsync();
        var orderId = await SeedOrderAsync(customer.User.Id, PaymentStatus.Paid, OrderStatus.Delivered);

        SetBearerToken(customer.AccessToken);

        // Act
        var response = await Client.PatchAsync(Route(orderId), null);

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var order = await GetOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        body!.Message.Should().Be("Sipariş teslim edildiği için iptal edilemez.");
        order.OrderStatus.Should().Be(OrderStatus.Delivered, "reddedilen istek durumu değiştirmemelidir.");
    }

    #endregion


    #region SETUP

    private sealed record OrderState(PaymentStatus PaymentStatus, OrderStatus OrderStatus, Guid? UserId);

    private static string Route(Guid orderId) => $"/api/orders/users/cancel/{orderId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private string CreateTokenWithoutUserIdClaim()
    {
        using var scope = Factory.Services.CreateScope();

        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: new[] { new Claim(ClaimTypes.Role, "USER") },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private async Task<Guid> SeedOrderAsync(Guid userId, PaymentStatus paymentStatus, OrderStatus orderStatus)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "İptal Test Ürün",
            Description = "İptal testleri için ürün",
            Slug = $"iptal-test-{Guid.NewGuid()}",
            Price = 150m,
            CategoryId = Guid.NewGuid(),
            IsDeleted = false
        };

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "İptal Test Kategori",
            Slug = $"iptal-kategori-{Guid.NewGuid()}"
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
