using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Cart.Commands.RemoveCartCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class RemoveCartCommandHandlerTests : BaseIntegrationTest
{
    public RemoveCartCommandHandlerTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
    }

    #region Success Tests

    [Fact]
    public async Task RemoveCart_WhenCurrentUserRequest_ShouldClearAllItemsAndInvalidateCache()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var otherUser = await CreateVanillaUserAsync();

        var product1 = await SeedProductAsync();
        var product2 = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product1.Id, 5);
        await SeedCartItemsAsync(cart.Id, product2.Id, 10);

        var otherCart = await SeedUserCartAsync(otherUser.User.Id);
        await SeedCartItemsAsync(otherCart.Id, product1.Id, 3);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cacheService.SetAsync(
            CacheKeys.UserCart(user.User.Id),
            "target cache",
            TimeSpan.FromHours(1));

        await cacheService.SetAsync(
            CacheKeys.UserCart(otherUser.User.Id),
            "other cache",
            TimeSpan.FromHours(1));

        var command = new RemoveCartCommand
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = user.User.Id,
            RestaurantTableId = null
        };

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS);

        var remainingItems = await ctx.CartItems
            .CountAsync(x => x.CartId == cart.Id);

        remainingItems.Should().Be(0,
            "kullanıcının sepetindeki bütün ürünler silinmelidir.");

        var otherItems = await ctx.CartItems
            .CountAsync(x => x.CartId == otherCart.Id);

        otherItems.Should().Be(1,
            "başka kullanıcının sepetindeki ürünler korunmalıdır.");

        var cartExists = await ctx.Carts
            .AnyAsync(x => x.Id == cart.Id);

        cartExists.Should().BeTrue(
            "sepet kaydı korunmalı, yalnızca ürünler silinmelidir.");

        var targetCache = await cacheService.GetAsync<string>(
            CacheKeys.UserCart(user.User.Id));

        var otherCache = await cacheService.GetAsync<string>(
            CacheKeys.UserCart(otherUser.User.Id));

        targetCache.Should().BeNull();
        otherCache.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveCart_WhenPanelUserRequest_ShouldClearOnlyTargetTableCart()
    {
        // Arrange
        var table = await SeedTableAsync();
        var otherTable = await SeedTableAsync();

        var product1 = await SeedProductAsync();
        var product2 = await SeedProductAsync();

        var cart = await SeedCartTableAsync(table.Id);

        await SeedCartItemsAsync(cart.Id, product1.Id, 5);
        await SeedCartItemsAsync(cart.Id, product2.Id, 10);

        var otherCart = await SeedCartTableAsync(otherTable.Id);
        await SeedCartItemsAsync(otherCart.Id, product1.Id, 3);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new RemoveCartCommand
        {
            RequestType = CartRequestTypes.PanelUser,
            RestaurantTableId = table.Id,
            UserId = null
        };

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS);

        var remainingItems = await ctx.CartItems
            .CountAsync(x => x.CartId == cart.Id);

        remainingItems.Should().Be(0,
            "hedef masanın sepetindeki bütün ürünler silinmelidir.");

        var otherItems = await ctx.CartItems
            .CountAsync(x => x.CartId == otherCart.Id);

        otherItems.Should().Be(1,
            "başka masanın sepetindeki ürünler korunmalıdır.");

        var cartExists = await ctx.Carts
            .AnyAsync(x => x.Id == cart.Id);

        cartExists.Should().BeTrue(
            "masa sepeti kaydı silinmemelidir.");
    }

    [Fact]
    public async Task RemoveCart_WhenCartIsAlreadyEmpty_ShouldReturnSuccess()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var cart = await SeedUserCartAsync(user.User.Id);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new RemoveCartCommand
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = user.User.Id,
            RestaurantTableId = null
        };

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS);

        var remainingItems = await ctx.CartItems
            .CountAsync(x => x.CartId == cart.Id);

        remainingItems.Should().Be(0);

        var cartExists = await ctx.Carts
            .AnyAsync(x => x.Id == cart.Id);

        cartExists.Should().BeTrue();
    }

    #endregion

    #region Error Tests

    [Fact]
    public async Task RemoveCart_WhenCartDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        // Kullanıcı var fakat sepeti oluşturulmadı.

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveCartCommand
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = user.User.Id,
            RestaurantTableId = null
        };

        // Act
        Func<Task> act = () => mediator.Send(command);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Sepet bulunamadı.");
    }

    #endregion

    #region Setup

    private async Task<RestaurantTable> SeedTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 3,
            IsActive = true,
            Number = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        ctx.RestaurantTables.Add(table);
        await ctx.SaveChangesAsync();

        return table;
    }

    private async Task<Cart> SeedCartTableAsync(Guid tableId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            UserId = null
        };

        ctx.Carts.Add(cart);
        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Cart> SeedUserCartAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantTableId = null
        };

        ctx.Carts.Add(cart);
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

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = $"Ana Yemekler {Guid.NewGuid():N}",
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

    private async Task<CartItem> SeedCartItemsAsync(
        Guid cartId, Guid productId, int quantity)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var item = new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };

        ctx.CartItems.Add(item);
        await ctx.SaveChangesAsync();

        return item;
    }

    #endregion
}
