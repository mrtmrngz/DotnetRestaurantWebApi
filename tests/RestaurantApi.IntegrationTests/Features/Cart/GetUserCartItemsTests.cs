using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Cart.Queries.GetCartItemQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class GetUserCartItemsTests: BaseIntegrationTest
{
    public const string Route = "/api/cart/users";
    
    public GetUserCartItemsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task GetUserCartItems_WhenUserHasCartItem_ShouldReturn200()
    {
        var userSetup = await CreateVanillaUserAsync();
        await SeedCartAsync(userSetup.User.Id);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Status kodu başarılı olmalıdyı.");

        var responseBody =
            await response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi");

        responseBody.Data.Should().HaveCount(1,
            "Response boyd içerisinde kullanının sepetindeki ürün miktarı kadar data olmalıydı.");
    }
    
    // SUCCESS TESTS END
    
    
    // ERROR TESTS START
    
    [Fact]
    public async Task GetUserCartItems_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için 401 hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task GetUserCartItems_WhenCartNotFound_ShouldReturn404()
    {
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kullanıcının sepeti olmadığı için 404 hata kodu dönmeliydi.");
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