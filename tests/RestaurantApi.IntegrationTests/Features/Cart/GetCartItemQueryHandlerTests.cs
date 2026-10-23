using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class GetCartItemQueryHandlerTests : BaseIntegrationTest
{
    public GetCartItemQueryHandlerTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
    }

    #region Success Tests

    [Fact]
    public async Task GetCartItems_WhenCurrentUserHasItems_ShouldReturnCorrectDetails()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 3);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().ContainSingle();

        var item = response.Data.Single();

        item.Title.Should().Be(product.Title);
        item.UnitPrice.Should().Be(320m);
        item.Quantity.Should().Be(3);
        item.DiscountRate.Should().Be(0m);
        item.SubTotal.Should().Be(960m);
        item.TotalPrice.Should().Be(960m);
    }

    [Fact]
    public async Task GetCartItems_WhenPanelUserHasItems_ShouldReturnCorrectDetails()
    {
        // Arrange
        var table = await SeedTableAsync();
        var product = await SeedProductAsync();
        var cart = await SeedCartTableAsync(table.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 2);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreatePanelQuery(table.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().ContainSingle();

        var item = response.Data.Single();

        item.Title.Should().Be(product.Title);
        item.UnitPrice.Should().Be(320m);
        item.Quantity.Should().Be(2);
        item.DiscountRate.Should().Be(0m);
        item.SubTotal.Should().Be(640m);
        item.TotalPrice.Should().Be(640m);
    }

    [Fact]
    public async Task GetCartItems_WhenCurrentUserHasMultipleItems_ShouldReturnAllItems()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        var product1 = await SeedProductAsync();
        var product2 = await SeedProductAsync();

        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product1.Id, 2);
        await SeedCartItemsAsync(cart.Id, product2.Id, 5);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().HaveCount(2);

        response.Data.Sum(x => x.Quantity).Should().Be(7);
        response.Data.Sum(x => x.SubTotal).Should().Be(2240m);
        response.Data.Sum(x => x.TotalPrice).Should().Be(2240m);
    }

    [Fact]
    public async Task GetCartItems_WhenCurrentUserHasNoItems_ShouldReturnEmptyList()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        await SeedUserCartAsync(user.User.Id);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    #endregion

    #region Data Isolation Tests

    [Fact]
    public async Task GetCartItems_WhenMultipleUsersExist_ShouldReturnOnlyTargetUserItems()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var otherUser = await CreateVanillaUserAsync();

        var product = await SeedProductAsync();

        var cart = await SeedUserCartAsync(user.User.Id);
        var otherCart = await SeedUserCartAsync(otherUser.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 2);
        await SeedCartItemsAsync(otherCart.Id, product.Id, 9);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Data.Should().ContainSingle();
        response.Data.Single().Quantity.Should().Be(2);
        response.Data.Single().TotalPrice.Should().Be(640m);
    }

    [Fact]
    public async Task GetCartItems_WhenMultipleTablesExist_ShouldReturnOnlyTargetTableItems()
    {
        // Arrange
        var table = await SeedTableAsync();
        var otherTable = await SeedTableAsync();

        var product = await SeedProductAsync();

        var cart = await SeedCartTableAsync(table.Id);
        var otherCart = await SeedCartTableAsync(otherTable.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 3);
        await SeedCartItemsAsync(otherCart.Id, product.Id, 8);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = CreatePanelQuery(table.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Data.Should().ContainSingle();
        response.Data.Single().Quantity.Should().Be(3);
        response.Data.Single().TotalPrice.Should().Be(960m);
    }

    #endregion

    #region Cache Tests

    [Fact]
    public async Task GetCartItems_WhenCacheMiss_ShouldStoreItemsInCache()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 4);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var cacheKey = CacheKeys.UserCart(user.User.Id);
        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        var cachedItems =
            await cacheService.GetAsync<List<GetCartQueryResult>>(cacheKey);

        // Assert
        response.Data.Should().ContainSingle();

        cachedItems.Should().NotBeNull();
        cachedItems.Should().ContainSingle();

        cachedItems!.Single().Quantity.Should().Be(4);
        cachedItems.Single().TotalPrice.Should().Be(1280m);
    }

    [Fact]
    public async Task GetCartItems_WhenCacheHit_ShouldReturnCachedItems()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 2);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var query = CreateCurrentUserQuery(user.User.Id);

        // İlk istek: DB'den okunup cache'e yazılır.
        var firstResponse = await mediator.Send(query);

        firstResponse.Data.Should().ContainSingle();
        firstResponse.Data.Single().Quantity.Should().Be(2);

        // Cache invalidation yapılmadan DB'deki miktar değiştirilir.
        var cartItem = await ctx.CartItems
            .SingleAsync(x => x.CartId == cart.Id);

        cartItem.Quantity = 7;
        await ctx.SaveChangesAsync();

        // Act
        var secondResponse = await mediator.Send(query);

        // Assert
        secondResponse.Data.Should().ContainSingle();

        secondResponse.Data.Single().Quantity.Should().Be(2,
            "cache hit durumunda veritabanındaki güncel miktar yerine cache verisi dönmelidir.");

        secondResponse.Data.Single().TotalPrice.Should().Be(640m);
    }

    #endregion

    #region Missing Cart Tests

    [Fact]
    public async Task GetCartItems_WhenUserCartDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        // Kullanıcı mevcut, fakat sepet kaydı hiç oluşturulmadı.

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCartItems_WhenTableCartDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        var table = await SeedTableAsync();
        // Masa mevcut, fakat sepet kaydı hiç oluşturulmadı.

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = CreatePanelQuery(table.Id);

        // Act
        var response = await mediator.Send(query);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().NotBeNull();
        response.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCartItems_WhenCartDoesNotExist_ShouldCacheEmptyList()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var cacheKey = CacheKeys.UserCart(user.User.Id);
        var query = CreateCurrentUserQuery(user.User.Id);

        // Act
        var response = await mediator.Send(query);
        var cachedItems =
            await cacheService.GetAsync<List<GetCartQueryResult>>(cacheKey);

        // Assert
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().BeEmpty();
        cachedItems.Should().NotBeNull();
        cachedItems.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static GetCartItemQuery CreateCurrentUserQuery(Guid userId)
    {
        return new GetCartItemQuery
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = userId,
            RestaurantTableId = null
        };
    }

    private static GetCartItemQuery CreatePanelQuery(Guid tableId)
    {
        return new GetCartItemQuery
        {
            RequestType = CartRequestTypes.PanelUser,
            UserId = null,
            RestaurantTableId = tableId
        };
    }

    #endregion

    #region Setup

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
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

    private async Task<Domain.Entities.Cart> SeedCartTableAsync(Guid tableId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            UserId = null
        };

        ctx.Carts.Add(cart);
        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Domain.Entities.Cart> SeedUserCartAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
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

        var category = new Domain.Entities.Category
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
            Price = 320m,
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
