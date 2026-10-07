using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Products;

public class DeleteProductTests : BaseIntegrationTest
{
    public DeleteProductTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task DeleteProduct_WhenProductExists_ShouldSoftDeleteAndClearCache()
    {
        var product = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde silinmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Silme işlemi sonucunda CONTENT_DELETED_SUCCESS kodu dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // db soft delete kontrolü
            var deletedProduct = await context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == product.Id);

            deletedProduct.Should().NotBeNull("Ürün veritabanında bulunmalıydı.");
            deletedProduct!.IsDeleted.Should().BeTrue("Ürün soft delete ile IsDeleted=true işaretlenmeliydi.");

            // cache kontrolü
            var publicProducts = await cache.GetAsync<string>(CacheKeys.PublicProducts());
            var adminProducts = await cache.GetAsync<string>(CacheKeys.AdminProducts());
            var publicProductDetail = await cache.GetAsync<string>(CacheKeys.PublicProductDetail(product.Slug));
            var adminProductDetail = await cache.GetAsync<string>(CacheKeys.AdminProductDetail(product.Id));

            publicProducts.Should().BeNull("Ürün silindiği için public ürün listesi redisten silinmeliydi.");
            adminProducts.Should().BeNull("Ürün silindiği için admin ürün listesi redisten silinmeliydi.");
            publicProductDetail.Should().BeNull("Ürün silindiği için public ürün detayı redisten silinmeliydi.");
            adminProductDetail.Should().BeNull("Ürün silindiği için admin ürün detayı redisten silinmeliydi.");
        }
    }

    [Fact]
    public async Task DeleteProduct_WhenProductNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Ürün bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "Response body içerisindeki code NOT_FOUND olmalıydı.");
    }

    [Fact]
    public async Task DeleteProduct_WhenProductAlreadyDeleted_ShouldReturn404()
    {
        var product = await Setup(isDeleted: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Ürün zaten silinmiş olduğu için 404 NOT FOUND dönmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "Response body içerisindeki code NOT_FOUND olmalıydı.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task DeleteProduct_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task DeleteProduct_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<Product> Setup(bool isDeleted = false)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var categoryMedia = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        context.Media.Add(categoryMedia);

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Ana Yemekler",
            Slug = "ana-yemekler",
            MediaId = categoryMedia.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Izgara Tavuk",
            Slug = "izgara-tavuk",
            Description = "Lezzetli ızgara tavuk",
            Price = 320,
            CategoryId = category.Id,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        await cache.SetAsync(CacheKeys.PublicProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.PublicProductDetail(product.Slug), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminProductDetail(product.Id), "prd", TimeSpan.FromHours(1));

        return product;
    }
}