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

public class SetCartItemQuantityForPanelTests: BaseIntegrationTest
{
    public SetCartItemQuantityForPanelTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START
    
    [Fact]
    public async Task SetCartItemQuantityForPanel_WhenValidData_ShouldReturn200()
    {
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTable();
        var ci = await SeedCartAsync(tableId);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/tables/{tableId}/items/{ci.Id}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "İşlem sonucunda, başarılı (200) durum kodu dönmeliydi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task SetCartItemQuantityForPanel_WhenUserNotLoggedIn_ShouldReturn401()
    {
        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/tables/{Guid.NewGuid()}/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için Unauthorized (401) hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task SetCartItemQuantityForPanel_WhenUserHasNotPermission_ShouldReturn403()
    {
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/tables/{Guid.NewGuid()}/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Kullanıcının bu rota için gerekli izni olmadığı için BadRequest (403) hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task SetCartItemQuantityForPanel_WhenValidationErrorAcquired_ShouldReturn400()
    {
        var userSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 150); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/tables/{Guid.NewGuid()}/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            "Validasyon hataları meydana geldiği için BadRequest (400) hata kodu dönmeliydi.");
    }

    [Fact]
    public async Task SetCartItemQuantityForPanel_WhenCartItemNotExist_ShouldReturn404()
    {
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTable();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        SetCartItemQuantityCommand command = new SetCartItemQuantityCommand(Quantity: 15); 

        var response = await Client.PatchAsJsonAsync($"/api/cart/tables/{tableId}/items/{Guid.NewGuid()}/quantity", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "İlgili ürün sepette olmadığı için NotFound (404) hata kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP
    private async Task<Guid> SeedTable()
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();
        
        Domain.Entities.RestaurantTable table = new Domain.Entities.RestaurantTable
        {
            Capacity = 2,
            IsActive = true,
            Number = 6,
            Id = Guid.NewGuid()
        };

        ctx.RestaurantTables.Add(table);

        await ctx.SaveChangesAsync();

        return table.Id;
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

    private async Task<CartItem> SeedCartAsync(Guid tableId)
    {
        var prd = await SeedProductAsync();
        
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        Domain.Entities.Cart cart = new Domain.Entities.Cart
        {
            RestaurantTableId = tableId,
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