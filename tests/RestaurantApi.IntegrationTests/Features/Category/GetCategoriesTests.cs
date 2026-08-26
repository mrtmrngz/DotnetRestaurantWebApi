using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Category.Queries.GetCategoriesQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Category;

public class GetCategoriesTests: BaseIntegrationTest
{
    public GetCategoriesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TESTS START

    [Fact]
    public async Task GetCategories_WhenCategoriesOnRedis_ShouldReturnCategories()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var categories = new List<GetCategoriesQueryResult>()
            {
                new GetCategoriesQueryResult()
                {
                    Id = Guid.NewGuid(),
                    Title = "Category 1",
                    Slug = "category-1",
                    ImageUrl = "cat1.png"
                },
                new GetCategoriesQueryResult()
                {
                    Id = Guid.NewGuid(),
                    Title = "Category 2",
                    Slug = "category-2",
                    ImageUrl = "cat2.png"
                }
            };

            await cacheService.SetAsync(CacheKeys.Categories(), categories, TimeSpan.FromHours(1));
        }

        var response = await Client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result.Data.Should().HaveCount(2, "2 adet kategori dönmeliydi.");
        
        // check first category

        result.Data[0].Title.Should().Be("Category 1", "Gelen category ile redisteki kategori title aynı olamlıydı.");
    }
    
    [Fact]
    public async Task GetCategories_WhenCategoriesNotExistOnRedisShouldFetchDbAndSetRedis_ShouldReturn200()
    {
        var categories = new List<GetCategoriesQueryResult>()
        {
            new GetCategoriesQueryResult()
            {
                Id = Guid.NewGuid(),
                Title = "Category 1",
                Slug = "category-1",
                ImageUrl = "cat1.png"
            },
            new GetCategoriesQueryResult()
            {
                Id = Guid.NewGuid(),
                Title = "Category 2",
                Slug = "category-2",
                ImageUrl = "cat2.png"
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
                    Id = categories[0].Id, Title = categories[0].Title, Slug = categories[0].Slug, MediaId = media[0].Id
                },
                new Domain.Entities.Category()
                {
                    Id = categories[1].Id, Title = categories[1].Title, Slug = categories[1].Slug, MediaId = media[1].Id
                }
            };
            
            apiContext.Categories.AddRange(catDb);
            await apiContext.SaveChangesAsync();
        }

        var response = await Client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetCategoriesQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result.Data.Should().HaveCount(2, "2 adet kategori dönmeliydi.");
        
        // check first category

        result.Data[0].Title.Should().Be(categories[0].Title, "Gelen category ile redisteki kategori title aynı olamlıydı.");
        
        // check cache
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cats = await cacheService.GetAsync<IReadOnlyList<GetCategoriesQueryResult>>(CacheKeys.Categories());

            cats.Should().NotBeNull("Kategoriler redise kaydedilmeliydi.");
            cats.Should().HaveCount(2, "Rediste 2 adet kategori olmalydı.");
            cats[0].Title.Should().Be(categories[0].Title,
                "Redisteki ilk kategori kaydedilen ilk kategori ile aynı olmalıydı.");
        }
    }
    
    // SUCCESS TESTS END
}