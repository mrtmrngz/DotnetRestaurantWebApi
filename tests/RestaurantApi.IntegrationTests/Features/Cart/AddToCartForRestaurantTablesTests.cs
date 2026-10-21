using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class AddToCartForRestaurantTablesTests: BaseIntegrationTest
{
    private const string Route = "/api/cart/tables/";
    
    public AddToCartForRestaurantTablesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task AddToCartForRestaurantTables_WhenValidData_ShouldReturn200()
    {
        var tableSetup = await SeedTableAsync();
        var productSetup = await SeedProductAsync();
        var userSetup = await CreateAdminUserAsync();

        AddToCartCommand command = new AddToCartCommand(ProductId: productSetup.Id, Quantity: 5);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsJsonAsync($"{Route}{tableSetup.Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Başarılı durum kodu dönmeliydi.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task AddToCartForRestaurantTables_WhenUserNotLoggedIn_ShouldReturn401()
    {
        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), Quantity: 5);

        var response = await Client.PostAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için 401 UNAUTHORIZED hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task AddToCartForRestaurantTables_WhenValidationErrorsAcquired_ShouldReturn400()
    {
        var userSetup = await CreateAdminUserAsync();
        
        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), Quantity: 500);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Validasyon hataları meydana geldiği için 400 BAD REQUEST hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task AddToCartForRestaurantTables_WhenUserHasNotPermissionForThisRoute_ShouldReturn403()
    {
        var userSetup = await CreateVanillaUserAsync();

        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), Quantity: 5);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının bu rota için gerekli izni olmadığı için 403 FORBIDDEN hata kodu dönmeliydi.");
    }
    
    // SETUP
    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var rt = new Domain.Entities.RestaurantTable()
        {
            Capacity = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            IsActive = true,
            Number = 2
        };
        
        ctx.RestaurantTables.Add(rt);

        await ctx.SaveChangesAsync();

        return rt;
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
}