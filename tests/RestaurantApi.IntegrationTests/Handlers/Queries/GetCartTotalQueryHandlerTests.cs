
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Queries;

public class GetCartTotalQueryHandlerTests : BaseIntegrationTest
{
    public GetCartTotalQueryHandlerTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
    }

    #region Empty Cart Tests

    [Fact]
    public async Task GetTotal_WhenUserHasNoCart_ShouldReturnAllZeros()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 0m, 0m, 0m, 0m, 0m);
    }

    [Fact]
    public async Task GetTotal_WhenTableHasNoCart_ShouldReturnAllZeros()
    {
        // Arrange
        var table = await SeedTableAsync();

        // Act
        var total = await GetTableTotalAsync(table.Id);

        // Assert
        AssertTotal(total, 0m, 0m, 0m, 0m, 0m);
    }

    [Fact]
    public async Task GetTotal_WhenCartExistsButHasNoItems_ShouldReturnAllZeros()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        await SeedUserCartAsync(user.User.Id);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 0m, 0m, 0m, 0m, 0m);
    }

    #endregion

    #region Shipping Tests

    [Fact]
    public async Task GetTotal_WhenSubtotalBelow500_ShouldAdd120Shipping()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync(p => p.Price = 320m);
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 320m, 320m, 0m, 120m, 440m);
    }

    [Fact]
    public async Task GetTotal_WhenSubtotalExactly500_ShouldHaveFreeShipping()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync(p => p.Price = 500m);
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 500m, 500m, 0m, 0m, 500m);
    }

    [Fact]
    public async Task GetTotal_WhenSubtotalAbove500_ShouldHaveFreeShipping()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync(p => p.Price = 320m);
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 2);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 640m, 640m, 0m, 0m, 640m);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public async Task GetTotal_WhenMultipleItemsExist_ShouldSumEveryItem()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        var firstProduct = await SeedProductAsync(p =>
        {
            p.Price = 320m;
            p.Title = "Izgara Tavuk";
        });

        var secondProduct = await SeedProductAsync(p =>
        {
            p.Price = 100m;
            p.Title = "Ayran";
        });

        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, firstProduct.Id, 1);
        await SeedCartItemsAsync(cart.Id, secondProduct.Id, 2);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        // 320 + (100 * 2) = 520
        AssertTotal(total, 520m, 520m, 0m, 0m, 520m);
    }

    [Fact]
    public async Task GetTotal_WhenPanelCartHasItems_ShouldCalculateTotal()
    {
        // Arrange
        var table = await SeedTableAsync();
        var product = await SeedProductAsync(p => p.Price = 320m);
        var cart = await SeedCartTableAsync(table.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);

        // Act
        var total = await GetTableTotalAsync(table.Id);

        // Assert
        AssertTotal(total, 320m, 320m, 0m, 120m, 440m);
    }

    #endregion

    #region Data Isolation Tests

    [Fact]
    public async Task GetTotal_WhenMultipleUsersExist_ShouldNotIncludeOtherUsersItems()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var otherUser = await CreateVanillaUserAsync();

        var product = await SeedProductAsync(p => p.Price = 320m);

        var cart = await SeedUserCartAsync(user.User.Id);
        var otherCart = await SeedUserCartAsync(otherUser.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);
        await SeedCartItemsAsync(otherCart.Id, product.Id, 5);

        // Act
        var total = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(total, 320m, 320m, 0m, 120m, 440m);
    }

    [Fact]
    public async Task GetTotal_WhenMultipleTablesExist_ShouldNotIncludeOtherTablesItems()
    {
        // Arrange
        var table = await SeedTableAsync();
        var otherTable = await SeedTableAsync();

        var product = await SeedProductAsync(p => p.Price = 320m);

        var cart = await SeedCartTableAsync(table.Id);
        var otherCart = await SeedCartTableAsync(otherTable.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);
        await SeedCartItemsAsync(otherCart.Id, product.Id, 5);

        // Act
        var total = await GetTableTotalAsync(table.Id);

        // Assert
        AssertTotal(total, 320m, 320m, 0m, 120m, 440m);
    }

    #endregion

    #region Database Freshness Tests

    [Fact]
    public async Task GetTotal_WhenProductPriceChanges_ShouldUseLatestDatabasePrice()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        var product = await SeedProductAsync(p => p.Price = 320m);
        var cart = await SeedUserCartAsync(user.User.Id);

        await SeedCartItemsAsync(cart.Id, product.Id, 1);

        // İlk hesaplama
        var before = await GetUserTotalAsync(user.User.Id);

        AssertTotal(before, 320m, 320m, 0m, 120m, 440m);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var dbProduct = await ctx.Products
                .SingleAsync(x => x.Id == product.Id);

            dbProduct.Price = 550m;

            await ctx.SaveChangesAsync();
        }

        // Act
        var after = await GetUserTotalAsync(user.User.Id);

        // Assert
        AssertTotal(after, 550m, 550m, 0m, 0m, 550m);
    }

    #endregion

    #region Query Helpers

    private async Task<GetCartTotalQueryResult> GetUserTotalAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new GetCartTotalQuery
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = userId,
            RestaurantTableId = null
        });

        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().NotBeNull();

        return response.Data;
    }

    private async Task<GetCartTotalQueryResult> GetTableTotalAsync(Guid tableId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new GetCartTotalQuery
        {
            RequestType = CartRequestTypes.PanelUser,
            UserId = null,
            RestaurantTableId = tableId
        });

        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        response.Data.Should().NotBeNull();

        return response.Data;
    }

    private static void AssertTotal(
        GetCartTotalQueryResult actual,
        decimal subtotal,
        decimal afterDiscount,
        decimal savings,
        decimal shipping,
        decimal total)
    {
        actual.Subtotal.Should().Be(subtotal);
        actual.AfterDiscountTotal.Should().Be(afterDiscount);
        actual.SavingTotal.Should().Be(savings);
        actual.ShippingCost.Should().Be(shipping);
        actual.Total.Should().Be(total);
    }

    #endregion

    #region Seed Helpers

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

    private async Task<Product> SeedProductAsync(
        Action<Product>? customConfig = null)
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
            Price = 320m,
            CategoryId = category.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        // Teste özel alanlar SaveChanges öncesinde ayarlanır.
        customConfig?.Invoke(product);

        ctx.Media.Add(media);
        ctx.Categories.Add(category);
        ctx.Products.Add(product);

        await ctx.SaveChangesAsync();

        return product;
    }

    private async Task<CartItem> SeedCartItemsAsync(
        Guid cartId,
        Guid productId,
        int quantity)
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
