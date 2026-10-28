using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Checkout;

public class CreateOnlineCheckoutCommandHandlerTests: BaseIntegrationTest
{
    public CreateOnlineCheckoutCommandHandlerTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenCurrentUserRequestIsValid_ShouldCreateOrderAndClearCartItems()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedDefaultAddressAsync(userSetup.User.Id);

        // 100 TL x 2 = 200 TL (500 TL altı olduğu için kargo 120 TL eklenir, toplam 320 TL)
        var seededCart = await SeedCartWithItemsAsync(userId: userSetup.User.Id, price: 100m, quantity: 2);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = "current-user-key-1"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        var response = await sender.Send(command, CancellationToken.None);

        // Assert
        response.Code.Should().Be(Codes.CHECKOUT_SESSION_CREATED);
        response.SessionUrl.Should().Be(FakePaymentService.SessionUrl);

        await using var assertScope = Factory.Services.CreateAsyncScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .Include(o => o.OrderItems)
            .SingleAsync();

        order.UserId.Should().Be(userSetup.User.Id);
        order.RestaurantTableId.Should().BeNull();
        order.IdempotencyKey.Should().Be(command.IdempotencyKey);
        order.OrderStatus.Should().Be(OrderStatus.Pending);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);

        order.CheckoutSessionId.Should().Be(FakePaymentService.SessionId);
        order.CheckoutSessionUrl.Should().Be(FakePaymentService.SessionUrl);
        order.CheckoutSessionExpiresAt.Should().BeAfter(DateTime.UtcNow);

        order.SubTotal.Should().Be(200m);
        order.ShippingCost.Should().Be(120m);
        order.Total.Should().Be(320m);

        order.AddressSnapshot.Should().NotBeNullOrWhiteSpace("kullanıcı siparişinde adres snapshot'ı kaydedilmelidir.");

        var orderItem = order.OrderItems.Should().ContainSingle().Subject;
        orderItem.ProductId.Should().Be(seededCart.ProductId);
        orderItem.UnitPrice.Should().Be(100m);
        orderItem.Quantity.Should().Be(2);
        orderItem.TotalPrice.Should().Be(200m);

        // ödeme oturumu açıldıktan sonra sepet kalemleri temizlenir, sepet kaydı korunur
        (await context.CartItems.CountAsync()).Should().Be(0, "sipariş sonrası sepet kalemleri temizlenmelidir.");
        (await context.Carts.CountAsync()).Should().Be(1, "sepet kaydı silinmemelidir.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenPanelUserRequestIsValid_ShouldCreateOrderWithoutShippingAndAddress()
    {
        // Arrange
        var table = await SeedTableAsync();

        // 600 TL (500 TL üzeri olduğu için masa siparişinde kargo yok)
        var seededCart = await SeedCartWithItemsAsync(tableId: table.Id, price: 600m, quantity: 1);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.PanelUser,
            RestaurantTableId = table.Id,
            IdempotencyKey = "panel-user-key-1"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        var response = await sender.Send(command, CancellationToken.None);

        // Assert
        response.Code.Should().Be(Codes.CHECKOUT_SESSION_CREATED);
        response.SessionUrl.Should().Be(FakePaymentService.SessionUrl);

        await using var assertScope = Factory.Services.CreateAsyncScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApiContext>();

        var order = await context.Orders
            .Include(o => o.OrderItems)
            .SingleAsync();

        order.UserId.Should().BeNull();
        order.RestaurantTableId.Should().Be(table.Id);
        order.IdempotencyKey.Should().Be(command.IdempotencyKey);
        order.CheckoutSessionId.Should().Be(FakePaymentService.SessionId);

        order.SubTotal.Should().Be(600m);
        order.ShippingCost.Should().Be(0m, "masa siparişlerinde kargo alınmaz.");
        order.Total.Should().Be(600m);
        order.AddressSnapshot.Should().BeNull("masa siparişinde adres snapshot'ı tutulmaz.");

        order.OrderItems.Should().ContainSingle().Subject.ProductId.Should().Be(seededCart.ProductId);

        (await context.CartItems.CountAsync()).Should().Be(0);
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenRequestTypeIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = (CheckoutRequestTypes)9999,
            UserId = Guid.NewGuid(),
            IdempotencyKey = "invalid-type-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();

        exception.Which.Errors.Should()
            .Contain(failure => failure.PropertyName == nameof(CreateOnlineCheckoutCommand.CheckoutRequestTypes) &&
                                failure.ErrorMessage == "Geçersiz ödeme işlem türü.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenTargetUserNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = Guid.NewGuid(),
            IdempotencyKey = "not-existing-user-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kullanıcı bulunamadı.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenIdempotencyKeyAlreadyUsed_ShouldThrowConflictException()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        const string idempotencyKey = "already-used-key";

        await SeedOrderWithIdempotencyKeyAsync(userSetup.User.Id, idempotencyKey);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = idempotencyKey
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Bu ödeme isteği daha önce başlatılmış. Lütfen ödeme işleminin bitmesini bekleyin.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenCartNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedDefaultAddressAsync(userSetup.User.Id);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = "no-cart-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Sepet bulunamadı.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenCartIsEmpty_ShouldThrowUnprocessableEntityError()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedDefaultAddressAsync(userSetup.User.Id);
        await SeedEmptyCartAsync(userSetup.User.Id);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = "empty-cart-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnprocessableEntityError>()
            .WithMessage("Sepetinizde sipariş oluşturulacak ürün bulunmuyor.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenCartContainsDeletedProduct_ShouldThrowConflictException()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedDefaultAddressAsync(userSetup.User.Id);
        await SeedCartWithItemsAsync(userId: userSetup.User.Id, price: 100m, quantity: 1, productDeleted: true);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = "deleted-product-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Sepette artık satışta olmayan ürünler var.");
    }

    [Fact]
    public async Task CreateOnlineCheckoutCommand_WhenCurrentUserHasNoAddress_ShouldThrowNotFoundException()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();
        await SeedCartWithItemsAsync(userId: userSetup.User.Id, price: 100m, quantity: 1);

        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = userSetup.User.Id,
            IdempotencyKey = "no-address-key"
        };

        await using var scope = CreateSenderScope(out var sender);

        // Act
        Func<Task> act = async () => await sender.Send(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }

    // ERROR TESTS END

    // SETUP

    private AsyncServiceScope CreateSenderScope(out ISender sender)
    {
        var scope = Factory.Services.CreateAsyncScope();

        sender = scope.ServiceProvider.GetRequiredService<ISender>();

        return scope;
    }

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

    private async Task<Guid> SeedEmptyCartAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        context.Carts.Add(cart);

        await context.SaveChangesAsync();

        return cart.Id;
    }

    private async Task<(Guid CartId, Guid ProductId)> SeedCartWithItemsAsync(Guid? userId = null,
        Guid? tableId = null, decimal price = 100m, int quantity = 1, bool productDeleted = false)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = CreateProduct(price, productDeleted);

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Test Kategori",
            Slug = $"test-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

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

        return (cart.Id, product.Id);
    }

    private async Task<Guid> SeedOrderWithIdempotencyKeyAsync(Guid userId, string idempotencyKey, decimal price = 100m)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var product = CreateProduct(price, isDeleted: false);

        context.Categories.Add(new Domain.Entities.Category
        {
            Id = product.CategoryId,
            Title = "Test Kategori",
            Slug = $"test-kategori-{Guid.NewGuid()}"
        });

        context.Products.Add(product);

        // sipariş kalemi ürün FK'sı için gerçek ürün gerekir, sepet kaydı veritabanına yazılmaz
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

        var order = Domain.Entities.Order.CreateFromCart(cart, userId, null, idempotencyKey, DateTime.UtcNow,
            OrderStatus.Pending);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }

    private static Domain.Entities.Product CreateProduct(decimal price, bool isDeleted)
    {
        return new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Test Ürün",
            Description = "Test ürün açıklaması",
            Slug = $"test-urun-{Guid.NewGuid()}",
            Price = price,
            CategoryId = Guid.NewGuid(),
            IsDeleted = isDeleted
        };
    }
}
