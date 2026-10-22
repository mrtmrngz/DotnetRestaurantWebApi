using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class RemoveCartItemForPanelTests: BaseIntegrationTest
{
    public RemoveCartItemForPanelTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START
    
    [Fact]
    public async Task RemoveCartItemForPanel_WhenValidData_ShouldReturn200()
    {
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTable();
        var ci = await SeedCartAsync(tableId);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.DeleteAsync($"/api/cart/tables/{tableId}/items/{ci.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "İşlem sonucunda, başarılı (200) durum kodu dönmeliydi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task RemoveCartItemForPanel_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var tableId = await SeedTable();
        var ci = await SeedCartAsync(tableId);

        var response = await Client.DeleteAsync($"/api/cart/tables/{tableId}/items/{ci.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için Unauthorized hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task RemoveCartItemForPanel_WhenUserHasNotPermission_ShouldReturn403()
    {
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.DeleteAsync($"/api/cart/tables/{Guid.NewGuid()}/items/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Kullanıcı bu işlem için yetkili olmadığı için Forbidden hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task RemoveCartItemForPanel_WhenRestaurantTableNotExist_ShouldReturn404()
    {
        var userSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.DeleteAsync($"/api/cart/tables/{Guid.NewGuid()}/items/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "Masa bulunamadığı için NotFound hata kodu dönmeliydi.");
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