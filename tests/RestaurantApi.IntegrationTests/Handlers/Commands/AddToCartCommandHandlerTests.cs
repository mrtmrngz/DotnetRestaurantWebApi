using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class AddToCartCommandHandlerTests: BaseIntegrationTest
{
    public AddToCartCommandHandlerTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task AddToCartCommandHandler_WhenValidDataForPanelCart_ShouldReturnSuccessBaseResponse()
    {
        var user = await CreateVanillaUserAsync();
        var table = await SeedTableAsync();
        var prd = await SeedProductAsync();
        await SaveCache(table.Id, user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.PanelUser, RestaurantTableId = table.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null olmamalı.");
        response.Code.Should().Be(Codes.ADD_TO_CART_SUCCESS,
            "Response içerisindeki kod ADD_TO_CART_SUCCESS olmalıydı.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // check cart

        var cart = await context.Carts.AsNoTracking().FirstOrDefaultAsync(x => x.RestaurantTableId == table.Id);

        cart.Should().NotBeNull("Cart null gelmemeliydi");

        var cartItem = await context.CartItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == prd.Id);

        cartItem.Should().NotBeNull("Sepetteki ürün null gelmemeliydi.");
        cartItem.Quantity.Should().Be(command.Quantity,
            "Sepetteki ürünün miktarı, command içerisindeki miktar ile aynı olmalıydı.");
        
        // check data redis
        var tableCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(table.Id));
        tableCache.Should().BeNull("Panel sepet cache null gelmesi gerekirdi.");
        
        var userCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(user.User.Id));
        userCache.Should().NotBeNull("User sepet cache null gelmemesi gerekirdi.");
    }
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenTableHasSameCartItem_ShouldIncreaseQuantityAndReturnSuccessBaseResponse()
    {
        var user = await CreateVanillaUserAsync();
        var table = await SeedTableAsync();
        var prd = await SeedProductAsync();
        var cart = await SeedCartTableAsync(table.Id);
        int testQuantity = 3;
        var crtItm = await SeedCartItemsAsync(cart.Id, prd.Id, testQuantity);
        await SaveCache(table.Id, user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.PanelUser, RestaurantTableId = table.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null olmamalı.");
        response.Code.Should().Be(Codes.ADD_TO_CART_SUCCESS,
            "Response içerisindeki kod ADD_TO_CART_SUCCESS olmalıydı.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // check cart

        var cartItem = await context.CartItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == crtItm.Id);

        cartItem.Should().NotBeNull("Sepetteki ürün null gelmemeliydi.");
        cartItem.Quantity.Should().Be(testQuantity + command.Quantity,
            "Sepetteki ürünün miktarı, command içerisindeki miktar ile sepetteki bulunana miktarın toplamı kadar olmalıydı.");
        
        // check data redis
        var tableCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(table.Id));
        tableCache.Should().BeNull("Panel sepet cache null gelmesi gerekirdi.");
        
        var userCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(user.User.Id));
        userCache.Should().NotBeNull("User sepet cache null gelmemesi gerekirdi.");
    }
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenValidDataForUserCart_ShouldReturnSuccessBaseResponse()
    {
        var user = await CreateVanillaUserAsync();
        var prd = await SeedProductAsync();
        Guid tableId = Guid.NewGuid();
        await SaveCache(tableId, user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = user.User.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null olmamalı.");
        response.Code.Should().Be(Codes.ADD_TO_CART_SUCCESS,
            "Response içerisindeki kod ADD_TO_CART_SUCCESS olmalıydı.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // check cart

        var cart = await context.Carts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.User.Id);

        cart.Should().NotBeNull("Cart null gelmemeliydi");

        // check cart
        var cartItem = await context.CartItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == prd.Id);

        cartItem.Should().NotBeNull("Sepetteki ürün null gelmemeliydi.");
        cartItem.Quantity.Should().Be(command.Quantity,
            "Sepetteki ürünün miktarı, command içerisindeki miktar kadar olmalıydı.");
        
        // check data redis
        var tableCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(tableId));
        tableCache.Should().NotBeNull("Panel sepet cache null gelmemesi gerekirdi.");
        
        var userCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(user.User.Id));
        userCache.Should().BeNull("User sepet cache null gelmesi gerekirdi.");
    }
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenUserHasCartItem_ShouldIncreaseQuantityAndReturnSuccessBaseResponse()
    {
        var user = await CreateVanillaUserAsync();
        var prd = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);
        int testQuantity = 3;
        var crtItm = await SeedCartItemsAsync(cart.Id, prd.Id, testQuantity);
        Guid tableId = Guid.NewGuid();
        await SaveCache(tableId, user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = user.User.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null olmamalı.");
        response.Code.Should().Be(Codes.ADD_TO_CART_SUCCESS,
            "Response içerisindeki kod ADD_TO_CART_SUCCESS olmalıydı.");

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // check cart

        var cartItem = await context.CartItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == crtItm.Id);

        cartItem.Should().NotBeNull("Sepetteki ürün null gelmemeliydi.");
        cartItem.Quantity.Should().Be(testQuantity + command.Quantity,
            "Sepetteki ürünün miktarı, command içerisindeki miktar ile sepetteki bulunana miktarın toplamı kadar olmalıydı.");
        
        // check data redis
        var tableCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(tableId));
        tableCache.Should().NotBeNull("Panel sepet cache null gelmemesi gerekirdi.");
        
        var userCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(user.User.Id));
        userCache.Should().BeNull("User sepet cache null gelmesi gerekirdi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenProductNotExist_ShouldThrow404()
    {
        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), Quantity: 3) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = Guid.NewGuid()
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenProductExistButIsDeleted_ShouldThrow404()
    {
        var prd = await SeedProductAsync(r => r.IsDeleted = true);
        
        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = Guid.NewGuid()
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenRequestIsCurrentUserButUserNotExist_ShouldThrow404()
    {
        var prd = await SeedProductAsync();

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = Guid.NewGuid()
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kullanıcı bulunamadı.");
    }

    [Fact]
    public async Task AddToCartCommandHandler_WhenRequestIsPanelCartButRestaurantTableNotFound_ShouldThrow404()
    {
        var prd = await SeedProductAsync();

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.PanelUser, RestaurantTableId = Guid.NewGuid()
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    } 
    
    [Fact]
    public async Task AddToCartCommandHandler_WhenRequestIsPanelCartAndRestaurantTableExistButTableIsDeleted_ShouldThrow404()
    {
        var prd = await SeedProductAsync();
        var table = await SeedTableAsync(r => r.IsActive = false);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 3) with
        {
            RequestType = CartRequestTypes.PanelUser, RestaurantTableId = table.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    } 

    [Fact]
    public async Task AddToCartCommandHandler_WhenExceedsMaxQuantityCount_ShouldThrowUnprocessableEntityException()
    {
        var user = await CreateVanillaUserAsync();
        var prd = await SeedProductAsync();
        var cart = await SeedUserCartAsync(user.User.Id);
        int testQuantity = 3;
        await SeedCartItemsAsync(cart.Id, prd.Id, testQuantity);
        Guid tableId = Guid.NewGuid();
        await SaveCache(tableId, user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, Quantity: 98) with
        {
            RequestType = CartRequestTypes.CurrentUser, RestaurantTableId = null, UserId = user.User.Id
        };

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<UnprocessableEntityError>()
            .WithMessage("Bir üründen sepete en fazla 100 adet eklenebilir. Daha fazlası için restoran ile iletişime geçiniz.");
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

    private async Task SaveCache(Guid tableId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.SetAsync(CacheKeys.PanelCart(tableId), "some data", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.UserCart(userId), "some data", TimeSpan.FromHours(1));
    }
}