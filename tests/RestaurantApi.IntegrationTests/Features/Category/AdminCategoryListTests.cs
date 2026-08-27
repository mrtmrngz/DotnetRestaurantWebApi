using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Category.Queries.AdminCategoryListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Category;

public class AdminCategoryListTests: BaseIntegrationTest
{
    public AdminCategoryListTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TESTS START

    [Fact]
    public async Task GetAdminCategories_WhenCategoriesOnRedis_ShouldReturnCategories()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var categories = new List<AdminCategoryListQueryResult>()
            {
                new AdminCategoryListQueryResult()
                {
                    Id = Guid.NewGuid(),
                    Title = "Category 1",
                    Slug = "category-1",
                    ImageUrl = "cat1.png",
                    IsDeleted = false
                },
                new AdminCategoryListQueryResult()
                {
                    Id = Guid.NewGuid(),
                    Title = "Category 2",
                    Slug = "category-2",
                    ImageUrl = "cat2.png",
                    IsDeleted = true
                }
            };

            await cacheService.SetAsync(CacheKeys.AdminCategories(), categories, TimeSpan.FromHours(1));
        }
        
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/categories/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result.Data.Should().HaveCount(2, "2 adet kategori dönmeliydi.");
        
        // check first category

        result.Data[0].Title.Should().Be("Category 1", "Gelen category ile redisteki kategori title aynı olamlıydı.");
    }
    
    [Fact]
    public async Task GetAdminCategories_WhenCategoriesNotExistOnRedisShouldFetchDbAndSetRedis_ShouldReturn200()
    {
        var categories = new List<AdminCategoryListQueryResult>()
        {
            new AdminCategoryListQueryResult()
            {
                Id = Guid.NewGuid(),
                Title = "Category 1",
                Slug = "category-1",
                ImageUrl = "cat1.png",
                IsDeleted = false,
            },
            new AdminCategoryListQueryResult()
            {
                Id = Guid.NewGuid(),
                Title = "Category 2",
                Slug = "category-2",
                ImageUrl = "cat2.png",
                IsDeleted = true,
            }
        };
        
        using (var scope = Factory.Services.CreateScope())
        {
            var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var media = new List<Media>()
            {
                new Media()
                {
                    Url = categories[0].ImageUrl, FileExtension = ".png", FileType = "img/png",
                    PublicId = Guid.NewGuid().ToString(), Size = 1024
                },
                new Media()
                {
                    Url = categories[1].ImageUrl, FileExtension = ".png", FileType = "img/png",
                    PublicId = Guid.NewGuid().ToString(), Size = 1024,
                }
            };
            
            apiContext.Media.AddRange(media);

            var catDb = new List<Domain.Entities.Category>()
            {
                new Domain.Entities.Category()
                {
                    Id = categories[0].Id, Title = categories[0].Title, Slug = categories[0].Slug, MediaId = media[0].Id,
                    IsDeleted = categories[0].IsDeleted
                },
                new Domain.Entities.Category()
                {
                    Id = categories[1].Id, Title = categories[1].Title, Slug = categories[1].Slug, MediaId = media[1].Id,IsDeleted = categories[1].IsDeleted
                }
            };
            
            apiContext.Categories.AddRange(catDb);
            await apiContext.SaveChangesAsync();
        }

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/categories/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<AdminCategoryListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result.Data.Should().HaveCount(2, "2 adet kategori dönmeliydi.");
        
        // check first category

        result.Data[0].Title.Should().Be(categories[0].Title, "Gelen category ile redisteki kategori title aynı olamlıydı.");
        
        // check cache
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cats = await cacheService.GetAsync<IReadOnlyList<AdminCategoryListQueryResult>>(CacheKeys.AdminCategories());

            cats.Should().NotBeNull("Kategoriler redise kaydedilmeliydi.");
            cats.Should().HaveCount(2, "Rediste 2 adet kategori olmalydı.");
            cats[0].Title.Should().Be(categories[0].Title,
                "Redisteki ilk kategori kaydedilen ilk kategori ile aynı olmalıydı.");
        }
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task GetAdminCategories_WhenUserNotAuthorize_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/categories/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Kullanıcı giriş yapmadığı için 401 hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task GetAdminCategories_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);
        
        var response = await Client.GetAsync("/api/categories/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Kullanıcı bu işlemi yapmak için yetkili olmadığı için 403 FORBIDDEN hatası dönmeliydi.");
    }
    
    // ERROR TESTS END
    
}