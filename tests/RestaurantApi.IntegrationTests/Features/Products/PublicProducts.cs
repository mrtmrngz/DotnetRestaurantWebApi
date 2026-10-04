using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Products.Queries.PublicProductsListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Products;

public class PublicProducts : BaseIntegrationTest
{
    public PublicProducts(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task PublicProducts_WhenProductsExistInDb_ShouldReturnFromDbAndSetRedis()
    {
        var (product, category, banner) = await Setup();

        var response = await Client.GetAsync("/api/products/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().HaveCount(1, "Veritabanında 1 adet ürün olduğu için 1 ürün dönmeliydi.");
        result.Data[0].Title.Should().Be(product.Title, "Ürün başlığı veritabanındaki ile aynı olmalıydı.");
        result.Data[0].CategoryName.Should().Be(category.Title, "Kategori adı ilişkili kategoriden gelmeliydi.");
        result.Data[0].Banner.Should().Be(banner.Url, "Banner, ana görselin url'i olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cached = await cacheService
                .GetAsync<IReadOnlyList<PublicProductsListQueryResult>>(CacheKeys.PublicProducts());

            cached.Should().NotBeNull("Veritabanından çekilen ürünler redise kaydedilmeliydi.");
            cached.Should().HaveCount(1, "Rediste 1 adet ürün olmalıydı.");
            cached![0].Title.Should().Be(product.Title, "Redisteki ürün veritabanındaki ile aynı olmalıydı.");
        }
    }

    [Fact]
    public async Task PublicProducts_WhenNoProductsExist_ShouldReturnEmptyArray()
    {
        var response = await Client.GetAsync("/api/products/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().NotBeNull("Data null gelmemeliydi.");
        result.Data.Should().BeEmpty("Veritabanında hiç ürün olmadığı için boş array dönmeliydi.");
    }

    [Fact]
    public async Task PublicProducts_WhenProductsOnRedis_ShouldReturnFromRedisWithoutDb()
    {
        var cachedProducts = new List<PublicProductsListQueryResult>
        {
            new PublicProductsListQueryResult
            {
                Id = Guid.NewGuid(),
                Title = "Redis Ürün 1",
                Slug = "redis-urun-1",
                Price = 50,
                CategoryName = "Redis Kategori",
                Banner = "redis-1.png"
            },
            new PublicProductsListQueryResult
            {
                Id = Guid.NewGuid(),
                Title = "Redis Ürün 2",
                Slug = "redis-urun-2",
                Price = 75,
                CategoryName = "Redis Kategori",
                Banner = "redis-2.png"
            }
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await cacheService.SetAsync(CacheKeys.PublicProducts(), cachedProducts, TimeSpan.FromHours(1));
        }

        var response = await Client.GetAsync("/api/products/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<PublicProductsListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().HaveCount(2, "Rediste 2 adet ürün olduğu için 2 ürün dönmeliydi.");
        result.Data[0].Title.Should().Be("Redis Ürün 1", "Gelen ürünler redisteki ürünlerle aynı olmalıydı.");
        result.Data[1].Title.Should().Be("Redis Ürün 2", "Gelen ürünler redisteki ürünlerle aynı olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            context.Products.Count().Should().Be(0,
                "Test için veritabanına hiç ürün kaydedilmedi; veri yalnızca redisten gelmeliydi.");
        }
    }

    // SUCCESS TESTS END

    // SETUP
    private async Task<(Product Product, Domain.Entities.Category Category, Media Banner)> Setup()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var categoryMedia = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        var bannerMedia = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "banner-1.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        context.Media.AddRange(categoryMedia, bannerMedia);

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
            Title = "Ürün 1",
            Slug = "urun-1",
            Description = "Ürün açıklaması",
            Price = 100,
            CategoryId = category.Id,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.ProductMedias.Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            MediaId = bannerMedia.Id,
            IsMain = true,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        return (product, category, bannerMedia);
    }
}