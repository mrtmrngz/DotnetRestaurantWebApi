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

public class CreateCheckoutForUsersTests: BaseIntegrationTest
{
    private const string Route = "/api/checkout/users";
    private const string IdempotencyHeaderName = "X-Idempotency-Key";

    public CreateCheckoutForUsersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCheckoutForUsers_WhenRequestIsValid_ShouldReturn200AndCreateCheckoutSession()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedDefaultAddressAsync(userSetup.User.Id);
        await SeedCartWithItemsAsync(userSetup.User.Id, price: 100m, quantity: 2);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "api-idempotency-key-1");

        // Act
        var response = await Client.PostAsync(Route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Kullanıcı, adres ve sepet uygun olduğu için ödeme oturumu açılmalıydı.");

        var responseBody = await response.ReadContentAsAsync<CheckoutResponse>();

        responseBody.Should().NotBeNull("Başarılı API isteğinde response body null gelmemeliydi.");
        responseBody!.Code.Should().Be(Codes.CHECKOUT_SESSION_CREATED);
        responseBody.SessionUrl.Should().Be(FakePaymentService.SessionUrl,
            "Ödeme sayfası adresi, açılan ödeme oturumundan dönmelidir.");

        // sipariş ve ödeme oturumu kaydedilmiş, sepet kalemleri temizlenmiş olmalı
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders.SingleAsync();

        order.UserId.Should().Be(userSetup.User.Id);
        order.CheckoutSessionId.Should().Be(FakePaymentService.SessionId);
        order.CheckoutSessionUrl.Should().Be(FakePaymentService.SessionUrl);

        (await context.CartItems.CountAsync()).Should().Be(0, "sipariş sonrası sepet kalemleri temizlenmelidir.");
        (await context.Carts.CountAsync()).Should().Be(1, "sepet kaydı silinmemelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateCheckoutForUsers_WhenUserIsNotAuthenticated_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "api-idempotency-key-401");

        // Act
        var response = await Client.PostAsync(Route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 Unauthorized dönmeliydi.");
    }

    [Fact]
    public async Task CreateCheckoutForUsers_WhenUserIsDeletedAfterLogin_ShouldReturn404()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);
        Client.DefaultRequestHeaders.Add(IdempotencyHeaderName, "api-idempotency-key-404");

        // token alındıktan sonra kullanıcı veritabanından silinir (token hâlâ teknik olarak geçerli)
        await DeleteUserAsync(userSetup.User.Id);

        // Act
        var response = await Client.PostAsync(Route, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kullanıcı artık veritabanında bulunmadığı için 404 Not Found dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<ErrorResponse>();

        responseBody.Should().NotBeNull("Hata yanıtında body null gelmemeliydi.");
        responseBody!.Message.Should().Be("Kullanıcı bulunamadı.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Guid> SeedDefaultAddressAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var address = new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            Title = "Ev",
            RecipientName = "Test User",
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

        context.Addresses.Add(address);

        await context.SaveChangesAsync();

        return address.Id;
    }

    private async Task<Guid> SeedCartWithItemsAsync(Guid userId, decimal price = 100m, int quantity = 1)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

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
            UserId = userId
        };

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

        return cart.Id;
    }

    private async Task DeleteUserAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var user = await context.Users.SingleAsync(x => x.Id == userId);

        context.Users.Remove(user);

        await context.SaveChangesAsync();
    }
}
