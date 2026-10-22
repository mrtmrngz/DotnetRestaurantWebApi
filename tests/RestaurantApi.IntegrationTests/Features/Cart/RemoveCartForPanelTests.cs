
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class RemoveCartForPanelTests : BaseIntegrationTest
{
    private const string Route = "/api/cart/tables/";

    public RemoveCartForPanelTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task RemoveCartForPanel_WhenValidRequest_ShouldReturn200()
    {
        // Arrange
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTableAsync();
        var cart = await SeedCartAsync(tableId);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.DeleteAsync($"{Route}{tableId}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "masa sepeti başarıyla temizlenmelidir.");

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var itemCount = await ctx.CartItems
            .CountAsync(x => x.CartId == cart.Id);

        itemCount.Should().Be(0,
            "masanın sepetindeki bütün ürünler silinmelidir.");

        var cartExists = await ctx.Carts
            .AnyAsync(x => x.Id == cart.Id);

        cartExists.Should().BeTrue(
            "Cart kaydı korunmalı, yalnızca sepet ürünleri silinmelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task RemoveCartForPanel_WhenUserNotLoggedIn_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync($"{Route}{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            "giriş yapmayan kullanıcı masa sepetini temizleyememelidir.");
    }

    [Fact]
    public async Task RemoveCartForPanel_WhenUserHasNotPermission_ShouldReturn403()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.DeleteAsync($"{Route}{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "gerekli izne sahip olmayan kullanıcı masa sepetini temizleyememelidir.");
    }

    [Fact]
    public async Task RemoveCartForPanel_WhenRestaurantTableNotFound_ShouldReturn404()
    {
        // Arrange
        var userSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.DeleteAsync($"{Route}{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.NotFound,
            "restoran masası bulunamadığında 404 dönmelidir.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Guid> SeedTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 2,
            IsActive = true,
            Number = 6
        };

        ctx.RestaurantTables.Add(table);
        await ctx.SaveChangesAsync();

        return table.Id;
    }

    private async Task<Domain.Entities.Cart> SeedCartAsync(Guid tableId)
    {
        var product = await SeedProductAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = null,
            RestaurantTableId = tableId
        };

        ctx.Carts.Add(cart);

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = 5
        };

        ctx.CartItems.Add(cartItem);

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
