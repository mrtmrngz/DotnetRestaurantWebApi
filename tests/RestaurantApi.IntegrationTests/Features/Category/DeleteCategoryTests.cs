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

namespace RestaurantApi.IntegrationTests.Features.Category;

public class DeleteCategoryTests: BaseIntegrationTest
{
    public DeleteCategoryTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TEST START

    [Fact]
    public async Task DeleteCategory_WhenCategoryDeleted_ShouldReturn200AndInvalidateRedis()
    {
        var targetCat = await SetupTest();
        var userSetupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetupResult.AccessToken);

        var response = await Client.DeleteAsync($"api/categories/{targetCat.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Kategori başarılı bir şekilde silindiği için 200 durum kodu dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            
            // verify category
            var category = await dbContext.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == targetCat.Id);
            category.Should().NotBeNull("Kategori null gelmemeliydi.");
            category.IsDeleted.Should().BeTrue("Kategori silinmeliydi.");
            
            // verify cache
            var adminCatsInCache = await cache.GetAsync<string>(CacheKeys.AdminCategories());
            var publicCatsInCache = await cache.GetAsync<string>(CacheKeys.Categories());
            adminCatsInCache.Should().BeNull("Başarılı silinmeden sonra kategoriler redisten silinmeliydi.");
            publicCatsInCache.Should().BeNull("Başarılı silinmeden sonra kategoriler redisten silinmeliydi.");
        }

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Silme işlemi başarılı olduğu için response body içerisindeki kod CONTENT_DELETED_SUCCESS gelmeliydi.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task DeleteCategory_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.DeleteAsync($"api/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için 401 UNAUTHORIZED hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task DeleteCategory_WhenUserNotAuthorized_ShouldReturn403()
    {
        var userSetupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetupResult.AccessToken);

        var response = await Client.DeleteAsync($"api/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı yetkisi olmadığı için 403 FORBIDDEN hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task DeleteCategory_WhenCategoryNotExist_ShouldReturn404()
    {
        var userSetupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetupResult.AccessToken);

        var response = await Client.DeleteAsync($"api/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kategoriye bulunamadığı için 404 NOT_FOUND hata kodu dönmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body.Code.Should().Be(Codes.NOT_FOUND,
            "Kategoriye bulunamadığı için respose body içerisinde NOT_FOUND hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task DeleteCategory_WhenCategoryAlreadyDeleted_ShouldReturn404()
    {
        var targetCat = await SetupTest(true);
        var userSetupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetupResult.AccessToken);

        var response = await Client.DeleteAsync($"api/categories/{targetCat.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kategoriye zaten silindiği için 404 NOT_FOUND hata kodu dönmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body.Code.Should().Be(Codes.NOT_FOUND,
            "Kategori hali hazırda silindiği için respose body içerisinde NOT_FOUND hata kodu dönmeliydi.");
    }

    [Fact]
    public async Task DeleteCategory_WhenCategoryHasActiveProduct_ShouldReturn422()
    {
        var targetCat = await SetupTest(false, true);
        var userSetupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetupResult.AccessToken);

        var response = await Client.DeleteAsync($"api/categories/{targetCat.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Kategoriye bağlı aktif ürünler olduğu için 422 UNPROCESSABLE_ENTITY durum kodu dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            
            // verify category
            var category = await dbContext.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == targetCat.Id);
            category.Should().NotBeNull("Kategori null gelmemeliydi.");
            category.IsDeleted.Should().BeFalse("Kategori silinmemeliydi.");
        }

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY,
            "Kategoriye bağlı aktif ürün olduğu için respose body içerisinde UNPROCESSABLE_ENTITY hata kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP
    private async Task<Domain.Entities.Category> SetupTest(bool isCategoryDeleted=false, bool isProductExist=false)
    {
        using var scope = Factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        var media = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            FileExtension = ".png",
            FileType = "image/png",
            Size = 133789,
            Url = $"http://localhost:9000/categories/{Guid.NewGuid()}"
        };

        dbContext.Media.Add(media);

        await dbContext.SaveChangesAsync();

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Tatlılar",
            Slug = "tatlilar",
            CreatedAt = DateTime.UtcNow,
            MediaId = media.Id,
            IsDeleted = isCategoryDeleted
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
        
        if (isProductExist)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                IsDeleted = false,
                Title = "Test product title",
                Slug = "Test slug title",
                CreatedAt = DateTime.UtcNow,
                AvgRate = 3,
                CommentCount = 5,
                Description = "Test desc",
                Price = 1478,
                TotalSold = 20,
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();
        }
        
        await cache.SetAsync(CacheKeys.Categories(), Guid.NewGuid().ToString(), TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminCategories(), Guid.NewGuid().ToString(), TimeSpan.FromHours(1));

        return category;
    }
}