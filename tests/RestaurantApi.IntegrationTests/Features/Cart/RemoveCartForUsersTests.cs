
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class RemoveCartForUsersTests : BaseIntegrationTest
{
    private const string Route = "/api/cart/users";

    public RemoveCartForUsersTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task RemoveCartForUsers_WhenValidRequest_ShouldReturn200()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        var cart = await SeedCartAsync(userSetup.User.Id);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "kullanıcının sepeti başarıyla temizlenmelidir.");

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var itemCount = await ctx.CartItems
            .CountAsync(x => x.CartId == cart.Id);

        itemCount.Should().Be(0);

        var cartExists = await ctx.Carts
            .AnyAsync(x => x.Id == cart.Id);

        cartExists.Should().BeTrue(
            "sepet kaydı silinmemeli, yalnızca ürünleri temizlenmelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task RemoveCartForUsers_WhenUserNotLoggedIn_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync(Route);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "giriş yapmayan kullanıcı sepet temizleme işlemi gerçekleştirememelidir.");
    }

    [Fact]
    public async Task RemoveCartForUsers_WhenCartNotFound_ShouldReturn404()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        // Kullanıcı var fakat sepet kaydı oluşturulmuyor.

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route);

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "kullanıcının sepeti bulunamadığında 404 dönmelidir.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Domain.Entities.Cart> SeedCartAsync(Guid userId)
    {
        var product = await SeedProductAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantTableId = null
        };

        ctx.Carts.Add(cart);

        var item = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = 5
        };

        ctx.CartItems.Add(item);

        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Product> SeedProductAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var media = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Ana Yemekler",
            Slug = $"ana-yemekler-{Guid.NewGuid():N}",
            MediaId = media.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Izgara Tavuk",
            Slug = $"izgara-tavuk-{Guid.NewGuid():N}",
            Description = "Lezzetli ızgara tavuk",
            Price = 320,
            CategoryId = category.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        ctx.Media.Add(media);
        ctx.Categories.Add(category);
        ctx.Products.Add(product);

        await ctx.SaveChangesAsync();

        return product;
    }
}
