using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class CartServiceTests: BaseIntegrationTest
{
    public CartServiceTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenUserExists_ShouldNotThrowAnything()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.CurrentUser, userSetup.User.Id, null, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("Kullanıcı veritabanında bulunduğu için hata fırlatılmamalıydı.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenTableExistsAndIsActive_ShouldNotThrowAnything()
    {
        // Arrange
        var table = await SeedTableAsync();

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.PanelUser, null, table.Id, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("Masa aktif olarak bulunduğu için hata fırlatılmamalıydı.");
    }

    [Fact]
    public async Task FindCartWithoutValidateForCheckout_WhenUserHasCart_ShouldReturnCartWithItemsProductsAndDiscounts()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        var cart = await SeedCartForUserAsync(userSetup.User.Id, discountRate: 20, quantity: 2);

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Domain.Entities.Cart? result = await cartService.FindCartWithoutValidateForCheckout(
            CheckoutRequestTypes.CurrentUser, userSetup.User.Id, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("Kullanıcının sepeti veritabanında bulunuyordu.");
        result!.Id.Should().Be(cart.Id);
        result.UserId.Should().Be(userSetup.User.Id);

        var cartItem = result.CartItems.Should().ContainSingle().Subject;
        cartItem.Quantity.Should().Be(2);
        cartItem.Product.Should().NotBeNull("sipariş fiyatlandırması için ürün bilgisi yüklenmelidir.");
        cartItem.Product.Discounts.Should().NotBeEmpty("indirimli fiyat hesabı için ürün indirimleri yüklenmelidir.");
    }

    [Fact]
    public async Task FindCartWithoutValidateForCheckout_WhenTableHasCart_ShouldReturnCartWithItems()
    {
        // Arrange
        var table = await SeedTableAsync();
        var cart = await SeedCartForTableAsync(table.Id, quantity: 3);

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Domain.Entities.Cart? result = await cartService.FindCartWithoutValidateForCheckout(
            CheckoutRequestTypes.PanelUser, null, table.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("Masanın sepeti veritabanında bulunuyordu.");
        result!.Id.Should().Be(cart.Id);
        result.RestaurantTableId.Should().Be(table.Id);
        result.CartItems.Should().ContainSingle().Subject.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task FindCartWithoutValidateForCheckout_WhenUserHasNoCart_ShouldReturnNull()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Domain.Entities.Cart? result = await cartService.FindCartWithoutValidateForCheckout(
            CheckoutRequestTypes.CurrentUser, userSetup.User.Id, null, CancellationToken.None);

        // Assert
        result.Should().BeNull("Kullanıcının henüz sepeti bulunmuyordu.");
    }

    [Fact]
    public async Task FindCartWithoutValidateForCheckout_WhenAnotherUserHasCart_ShouldNotReturnThatCart()
    {
        // Arrange
        var ownerSetup = await CreateVanillaUserAsync();
        var anotherUserSetup = await CreateVanillaUserAsync();

        await SeedCartForUserAsync(ownerSetup.User.Id);

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Domain.Entities.Cart? result = await cartService.FindCartWithoutValidateForCheckout(
            CheckoutRequestTypes.CurrentUser, anotherUserSetup.User.Id, null, CancellationToken.None);

        // Assert
        result.Should().BeNull("Sepet yalnızca sahibi olan kullanıcı için dönmelidir.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenUserNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var notExistingUserId = Guid.NewGuid();

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.CurrentUser, notExistingUserId, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kullanıcı bulunamadı.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenUserIdIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.CurrentUser, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenTableNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var notExistingTableId = Guid.NewGuid();

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.PanelUser, null, notExistingTableId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenTableIsPassive_ShouldThrowNotFoundException()
    {
        // Arrange
        var passiveTable = await SeedTableAsync(isActive: false);

        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.PanelUser, null, passiveTable.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.",
                "Pasif masa sipariş alabilen masa olarak kabul edilmez.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenTableIdIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            CheckoutRequestTypes.PanelUser, null, null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }

    [Fact]
    public async Task ValidateTargetEntityForCheckoutAsync_WhenRequestTypeIsInvalid_ShouldThrowBadRequestException()
    {
        // Arrange
        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.ValidateTargetEntityForCheckoutAsync(
            (CheckoutRequestTypes)99, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Geçersiz sepet istek tipi.");
    }

    [Fact]
    public async Task FindCartWithoutValidateForCheckout_WhenRequestTypeIsInvalid_ShouldThrowBadRequestException()
    {
        // Arrange
        await using var scope = CreateCartServiceScope(out var cartService);

        // Act
        Func<Task> act = async () => await cartService.FindCartWithoutValidateForCheckout(
            (CheckoutRequestTypes)99, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Geçersiz işlem tipi.");
    }

    // ERROR TESTS END

    // SETUP

    private AsyncServiceScope CreateCartServiceScope(out ICartService cartService)
    {
        var scope = Factory.Services.CreateAsyncScope();

        cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        return scope;
    }

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(bool isActive = true)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = Random.Shared.Next(1, 10_000),
            Capacity = 6,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    private async Task<Domain.Entities.Cart> SeedCartForUserAsync(Guid userId, int? discountRate = null,
        int quantity = 1)
    {
        return await SeedCartAsync(userId: userId, tableId: null, discountRate: discountRate, quantity: quantity);
    }

    private async Task<Domain.Entities.Cart> SeedCartForTableAsync(Guid tableId, int? discountRate = null,
        int quantity = 1)
    {
        return await SeedCartAsync(userId: null, tableId: tableId, discountRate: discountRate, quantity: quantity);
    }

    private async Task<Domain.Entities.Cart> SeedCartAsync(Guid? userId, Guid? tableId, int? discountRate,
        int quantity)
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
            Price = 100m,
            CategoryId = category.Id
        };

        context.Categories.Add(category);
        context.Products.Add(product);

        if (discountRate.HasValue)
        {
            context.Discounts.Add(new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Rate = discountRate.Value,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1)
            });
        }

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RestaurantTableId = tableId
        };

        context.Carts.Add(cart);

        context.CartItems.Add(new Domain.Entities.CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = quantity
        });

        await context.SaveChangesAsync();

        return cart;
    }
}
