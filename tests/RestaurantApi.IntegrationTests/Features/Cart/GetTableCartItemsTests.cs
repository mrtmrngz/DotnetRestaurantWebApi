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

public class GetTableCartItemsTests: BaseIntegrationTest
{
    private const string Route = "/api/cart/tables/";
    
    public GetTableCartItemsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task GetTableCartItems_WhenValidRequest_ShouldReturn200()
    {
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTableAsync();
        await SeedCartAsync(tableId);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.GetAsync($"{Route}{tableId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Status kodu başarılı olmalıdyı.");

        var responseBody =
            await response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetCartQueryResult>>>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi");

        responseBody.Data.Should().HaveCount(1,
            "Response body içerisinde ürünlerin sayısı, masa sepetindeki ürünlerin sayısı kadar data olmalıydı.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task GetTableCartItems_WhenUserNotLoggedId_ShouldReturn401()
    {
        var response = await Client.GetAsync($"{Route}{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcının giriş yapmadığı için 401 hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task GetTableCartItems_WhenUserHasNotPermission_ShouldReturn403()
    {
        var userSetup = await CreateVanillaUserAsync();
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.GetAsync($"{Route}{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının bu işlemi yapabilmesi için gerekli izne sahip olmadığı için 403 hata kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP

    private async Task<Guid> SeedTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 2,
            IsActive = true,
            Number = 6
        };

        ctx.RestaurantTables.Add(table);
        await ctx.SaveChangesAsync();

        return table.Id;
    }

    private async Task<Domain.Entities.Cart> SeedCartAsync(Guid tableId)
    {
        var product = await SeedProductAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = null,
            RestaurantTableId = tableId
        };

        ctx.Carts.Add(cart);

        var cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = 5
        };

        ctx.CartItems.Add(cartItem);

        await ctx.SaveChangesAsync();

        return cart;
    }

    private async Task<Product> SeedProductAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var media = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Ana Yemekler",
            Slug = $"ana-yemekler-{Guid.NewGuid():N}",
            MediaId = media.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Izgara Tavuk",
            Slug = $"izgara-tavuk-{Guid.NewGuid():N}",
            Description = "Lezzetli ızgara tavuk",
            Price = 320,
            CategoryId = category.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        ctx.Media.Add(media);
        ctx.Categories.Add(category);
        ctx.Products.Add(product);

        await ctx.SaveChangesAsync();

        return product;
    }
}