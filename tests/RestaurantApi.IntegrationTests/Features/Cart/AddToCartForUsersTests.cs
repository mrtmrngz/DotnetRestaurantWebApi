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

public class AddToCartForUsersTests: BaseIntegrationTest
{
    private const string Route = "/api/cart/users";
    
    public AddToCartForUsersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task AddToCartForUsers_WhenValidData_ShouldReturn200()
    {
        var user = await CreateVanillaUserAsync();
        var prd = await SeedProductAsync();
        await SaveCache(user.User.Id);

        AddToCartCommand command = new AddToCartCommand(ProductId: prd.Id, 3);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var response = await Client.PostAsJsonAsync(Route, command);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Başarılı durum kodu dönmeliydi.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task AddToCartForUsers_WhenValidationErrorsAcquired_ShouldReturn400()
    {
        var user = await CreateVanillaUserAsync();

        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), 999);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        var response = await Client.PostAsJsonAsync(Route, command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Validasyon hataları meydana geldiği için 400 Bad request hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task AddToCartForUsers_WhenUserNotLoggedIn_ShouldReturn400()
    {

        AddToCartCommand command = new AddToCartCommand(ProductId: Guid.NewGuid(), 999);

        var response = await Client.PostAsJsonAsync(Route, command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için 401 Unauthorized hata kodu dönmeliydi.");
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
    private async Task SaveCache(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.SetAsync(CacheKeys.UserCart(userId), "some data", TimeSpan.FromHours(1));
    }
}