using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Cart.Commands.RemoveCartItemCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class RemoveCartItemCommandHandlerTests : BaseIntegrationTest
{
    public RemoveCartItemCommandHandlerTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task
        RemoveCartItemCommandHandler_WhenCurrentUserRequestAndValid_ShouldReturnSuccessResponseAndDeleteCorrectItem()
    {
        var user = await CreateVanillaUserAsync();
        var prd = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);
        var cartItem = await SeedCartItemsAsync(cart.Id, prd.Id, 5);

        var otherUser = await CreateVanillaUserAsync();
        var otherCart = await SeedUserCartAsync(otherUser.User.Id);
        var otherCartItem = await SeedCartItemsAsync(otherCart.Id, prd.Id, 10);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cacheService.SetAsync(CacheKeys.UserCart(user.User.Id), "target user cache", TimeSpan.FromHours(1));
        await cacheService.SetAsync(CacheKeys.UserCart(otherUser.User.Id), "other user cache", TimeSpan.FromHours(1));

        RemoveCartItemCommand
            command = new RemoveCartItemCommand() with
            {
                RequestType = CartRequestTypes.CurrentUser,
                UserId = user.User.Id,
                ItemId = cartItem.Id
            };

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeliydi");
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Response içersindeki kod CONTENT_DELETED_SUCCESS gelmeliydi");

        // item validation

        var item = await apiContext.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cartItem.Id);
        var otherItem = await apiContext.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == otherCartItem.Id);

        item.Should().BeNull("Kullanıcnın sepetteki ürünü silinmiş olmalıydı.");
        otherItem.Should().NotBeNull("Başka kullanıcının, sepetteki ürünü silinmemiş olmalıydı.");

        // cache validation
        var targetCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(user.User.Id));
        var otherCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(otherUser.User.Id));

        targetCache.Should().BeNull("Sepetteki ürünü silinen kullanıcının redisteki cachesi silinmeliydi.");
        otherCache.Should().NotBeNull("Diğer kullanıcının sepeti, redisten silinmemeliydi.");
    }
    
    [Fact]
    public async Task
        RemoveCartItemCommandHandler_WhenPanelRequestAndValid_ShouldReturnSuccessResponseAndDeleteCorrectItem()
    {
        var prd = await SeedProductAsync();
        var table = await SeedTableAsync();
        var cart = await SeedCartTableAsync(table.Id);
        var cartItem = await SeedCartItemsAsync(cart.Id, prd.Id, 5);

        var otherTable = await SeedTableAsync();
        var otherCart = await SeedCartTableAsync(otherTable.Id);
        var otherCartItem = await SeedCartItemsAsync(otherCart.Id, prd.Id, 10);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        RemoveCartItemCommand
            command = new RemoveCartItemCommand() with
            {
                RequestType = CartRequestTypes.PanelUser,
                RestaurantTableId = table.Id,
                ItemId = cartItem.Id
            };

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeliydi");
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Response içersindeki kod CONTENT_DELETED_SUCCESS gelmeliydi");

        var item = await apiContext.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cartItem.Id);
        var otherItem = await apiContext.CartItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == otherCartItem.Id);

        item.Should().BeNull("Masanın sepetteki ürünü silinmiş olmalıydı.");
        otherItem.Should().NotBeNull("Başka masanın, sepetteki ürünü silinmemiş olmalıydı.");
    }

    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task RemoveCartItemCommandCommandHandler_WhenValidationErrorsAcquired_ShouldThrowValidationException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        RemoveCartItemCommand
            command = new RemoveCartItemCommand() with
            {
                RequestType = CartRequestTypes.PanelUser, ItemId = Guid.NewGuid(), UserId = Guid.NewGuid()
            };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should()
            .ThrowAsync<ValidationException>();
    }
    
    [Fact]
    public async Task RemoveCartItemCommandHandler_WhenUserTargetsAnotherUserCartItem_ShouldThrow404()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        await SeedUserCartAsync(user.User.Id);

        var otherUser = await CreateVanillaUserAsync();
        var otherCart = await SeedUserCartAsync(otherUser.User.Id);

        var product = await SeedProductAsync();
        var otherCartItem = await SeedCartItemsAsync(
            otherCart.Id, product.Id, 5);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        RemoveCartItemCommand command = new RemoveCartItemCommand()
        {
            RequestType = CartRequestTypes.CurrentUser,
            ItemId = otherCartItem.Id,
            UserId = user.User.Id,
            RestaurantTableId = null
        };

        // Act
        Func<Task> act = () => mediator.Send(command);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Sepetteki ürün bulunamadı.");

        var item = await ctx.CartItems
            .AsNoTracking()
            .SingleAsync(x => x.Id == otherCartItem.Id);

        item.Should().NotBeNull("Kullanıcı, başka kullanıcının sepetteki ürünü silmememeliydi.");
    }
    
    [Fact]
    public async Task RemoveCartItemCommandHandler_WhenUserTargetsTableCartItem_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();
        await SeedUserCartAsync(user.User.Id);

        var product = await SeedProductAsync();
        var table = await SeedTableAsync();
        var tableCart = await SeedCartTableAsync(table.Id);
        var tableCartItem = await SeedCartItemsAsync(
            tableCart.Id, product.Id, 5);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        RemoveCartItemCommand command = new RemoveCartItemCommand()
        {
            RequestType = CartRequestTypes.CurrentUser,
            ItemId = tableCartItem.Id,
            UserId = user.User.Id,
            RestaurantTableId = null
        };

        // Act
        Func<Task> act = () => mediator.Send(command);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Sepetteki ürün bulunamadı.");

        var item = await ctx.CartItems
            .AsNoTracking()
            .SingleAsync(x => x.Id == tableCartItem.Id);

        item.Should().NotBeNull("Kullanıcı, masanın sepetteki ürününü silmemeliydi.");
    }
    
    [Fact]
    public async Task RemoveCartItemCommand_WhenPanelTargetsAnotherTableCartItem_ShouldThrow404()
    {
        // Arrange
        var table = await SeedTableAsync();
        await SeedCartTableAsync(table.Id);

        var otherTable = await SeedTableAsync();
        var otherCart = await SeedCartTableAsync(otherTable.Id);

        var product = await SeedProductAsync();
        var otherCartItem = await SeedCartItemsAsync(
            otherCart.Id, product.Id, 5);

        await using var scope = Factory.Services.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        RemoveCartItemCommand command = new RemoveCartItemCommand()
        {
            RequestType = CartRequestTypes.PanelUser,
            ItemId = otherCartItem.Id,
            UserId = null,
            RestaurantTableId = table.Id
        };

        // Act
        Func<Task> act = () => mediator.Send(command);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Sepetteki ürün bulunamadı.");

        var item = await ctx.CartItems
            .AsNoTracking()
            .SingleAsync(x => x.Id == otherCartItem.Id);

        item.Should().NotBeNull("İlgili masa harici başka bir masanın sepetteki ürünü silinmemeliydi.");
    }

    [Fact]
    public async Task RemoveCartItemCommandCommandHandler_WhenCartItemNotExist_ShouldThrowNotFoundException()
    {
        var user = await CreateVanillaUserAsync();
        await SeedProductAsync();
        await SeedUserCartAsync(user.User.Id);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        RemoveCartItemCommand
            command = new RemoveCartItemCommand() with
            {
                RequestType = CartRequestTypes.CurrentUser, ItemId = Guid.NewGuid(), UserId = user.User.Id
            };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should()
            .ThrowAsync<NotFoundException>(
                "Sepette böyle bir ürün olmadığı için NotFoundException hatasını fırlatmalıydı.")
            .WithMessage("Sepetteki ürün bulunamadı.");
    }
    
    // ERROR TESTS END

    // SETUP
    private async Task<RestaurantTable> SeedTableAsync(Action<RestaurantTable>? customConfig = null)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var rt = new RestaurantTable
        {
            Capacity = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IsActive = true,
            Number = 2
        };

        customConfig?.Invoke(rt);

        ctx.RestaurantTables.Add(rt);

        await ctx.SaveChangesAsync();

        return rt;
    }

    private async Task<Cart> SeedCartTableAsync(Guid tableId)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Cart
        {
            RestaurantTableId = tableId
        };

        ctx.Carts.Add(cart);

        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Cart> SeedUserCartAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Cart
        {
            UserId = userId
        };

        ctx.Carts.Add(cart);

        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Product> SeedProductAsync(Action<Product>? customConfig = null)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var categoryMedia = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        ctx.Media.Add(categoryMedia);

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Ana Yemekler",
            Slug = "ana-yemekler",
            MediaId = categoryMedia.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        ctx.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Izgara Tavuk",
            Slug = "izgara-tavuk",
            Description = "Lezzetli ızgara tavuk",
            Price = 320,
            CategoryId = category.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        customConfig?.Invoke(product);

        ctx.Products.Add(product);

        await ctx.SaveChangesAsync();

        return product;
    }

    private async Task<CartItem> SeedCartItemsAsync(Guid cartId, Guid productId, int quantity)
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cI = new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };

        ctx.CartItems.Add(cI);

        await ctx.SaveChangesAsync();

        return cI;
    }
}