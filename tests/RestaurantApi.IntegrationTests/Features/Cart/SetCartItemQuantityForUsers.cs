using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Cart.Commands.SetCartItemQuantityCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class SetCartItemQuantityForUsers: BaseIntegrationTest
{
    
    public SetCartItemQuantityForUsers(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task SetCartItemQuantityForUsers_WhenValidData_ShouldReturn200()
    {
        var userSetup = await CreateVanillaUserAsync();
        var ci = await SeedCartAsync(userSetup.User.Id);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/users/items/{ci.Id}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "İşlem sonucunda, başarılı (200) durum kodu dönmeliydi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task SetCartItemQuantityForUsers_WhenUserNotLoggedIn_ShouldReturn401()
    {
        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/users/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için Unauthorized (401) hata durum kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task SetCartItemQuantityForUsers_WhenInvalidData_ShouldReturn400()
    {
        var userSetup = await CreateVanillaUserAsync();
        var ci = await SeedCartAsync(userSetup.User.Id);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 150); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/users/items/{ci.Id}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            "Validasyon hataları meydana geliceği için 400 BAD REQUEST hata kodu dönemliydi.");
    }
    
    [Fact]
    public async Task SetCartItemQuantityForUsers_WhenCartItemNotExist_ShouldReturn404()
    {
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/users/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "Sepet/Sepetteki ürün bulunamadığı için NotFound (404) hata durum kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP
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

    private async Task<CartItem> SeedCartAsync(Guid userId)
    {
        var prd = await SeedProductAsync();
        
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        Domain.Entities.Cart cart = new Domain.Entities.Cart
        {
            UserId = userId,
            Id = Guid.NewGuid()
        };

        ctx.Carts.Add(cart);

        CartItem ct = new CartItem
        {
            ProductId = prd.Id,
            CartId = cart.Id,
            Quantity = 5
        };

        ctx.CartItems.Add(ct);

        await ctx.SaveChangesAsync();

        return ct;
    }
}