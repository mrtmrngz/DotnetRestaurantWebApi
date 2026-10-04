using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Products.Queries.AdminProductListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Products;

public class AdminProductsTests : BaseIntegrationTest
{
    public AdminProductsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task AdminProducts_WhenProductsExistInDb_ShouldReturnFromDbWithIsDeletedAndSetRedis()
    {
        var (activeProduct, deletedProduct, category, banner) = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().HaveCount(2, "Admin, silinmiş ürünler dahil tüm ürünleri görmeliydi.");

        var active = result.Data.Single(x => x.Id == activeProduct.Id);
        active.Title.Should().Be(activeProduct.Title, "Ürün başlığı veritabanındaki ile aynı olmalıydı.");
        active.CategoryName.Should().Be(category.Title, "Kategori adı ilişkili kategoriden gelmeliydi.");
        active.Banner.Should().Be(banner.Url, "Banner, ana görselin url'i olmalıydı.");
        active.IsDeleted.Should().BeFalse("Aktif ürün için IsDeleted false dönmeliydi.");

        var deleted = result.Data.Single(x => x.Id == deletedProduct.Id);
        deleted.IsDeleted.Should().BeTrue(
            "IgnoreQueryFilters sayesinde silinmiş ürün IsDeleted=true ile dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cached = await cacheService
                .GetAsync<IReadOnlyList<AdminProductListQueryResult>>(CacheKeys.AdminProducts());

            cached.Should().NotBeNull("Veritabanından çekilen ürünler redise kaydedilmeliydi.");
            cached.Should().HaveCount(2, "Rediste 2 adet ürün olmalıydı.");
        }
    }

    [Fact]
    public async Task AdminProducts_WhenNoProductsExist_ShouldReturnEmptyArray()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().NotBeNull("Data null gelmemeliydi.");
        result.Data.Should().BeEmpty("Veritabanında hiç ürün olmadığı için boş array dönmeliydi.");
    }

    [Fact]
    public async Task AdminProducts_WhenProductsOnRedis_ShouldReturnFromRedisWithoutDb()
    {
        var setupResult = await CreateAdminUserAsync();

        var cachedProducts = new List<AdminProductListQueryResult>
        {
            new AdminProductListQueryResult
            {
                Id = Guid.NewGuid(),
                Title = "Redis Ürün 1",
                Slug = "redis-urun-1",
                Price = 50,
                CategoryName = "Redis Kategori",
                Banner = "redis-1.png",
                IsDeleted = false
            },
            new AdminProductListQueryResult
            {
                Id = Guid.NewGuid(),
                Title = "Redis Ürün 2",
                Slug = "redis-urun-2",
                Price = 75,
                CategoryName = "Redis Kategori",
                Banner = "redis-2.png",
                IsDeleted = true
            }
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await cacheService.SetAsync(CacheKeys.AdminProducts(), cachedProducts, TimeSpan.FromHours(1));
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<AdminProductListQueryResult>>>();

        result.Should().NotBeNull("Response body boş gelmemeliydi.");
        result!.Data.Should().HaveCount(2, "Rediste 2 adet ürün olduğu için 2 ürün dönmeliydi.");
        result.Data[0].Title.Should().Be("Redis Ürün 1", "Gelen ürünler redisteki ürünlerle aynı olmalıydı.");
        result.Data[1].Title.Should().Be("Redis Ürün 2", "Gelen ürünler redisteki ürünlerle aynı olmalıydı.");
        result.Data[1].IsDeleted.Should().BeTrue("Redisten gelen silinmiş ürün IsDeleted=true olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            context.Products.Count().Should().Be(0,
                "Test için veritabanına hiç ürün kaydedilmedi; veri yalnızca redisten gelmeliydi.");
        }
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task AdminProducts_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task AdminProducts_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<(Product ActiveProduct, Product DeletedProduct, Domain.Entities.Category Category, Media ActiveBanner)> Setup()
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

        var activeBanner = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "banner-1.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        var deletedBanner = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "banner-2.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        context.Media.AddRange(categoryMedia, activeBanner, deletedBanner);

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

        var activeProduct = new Product
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

        var deletedProduct = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Ürün 2",
            Slug = "urun-2",
            Description = "Ürün açıklaması",
            Price = 200,
            CategoryId = category.Id,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.AddRange(activeProduct, deletedProduct);
        await context.SaveChangesAsync();

        context.ProductMedias.AddRange(
            new ProductMedia
            {
                Id = Guid.NewGuid(),
                ProductId = activeProduct.Id,
                MediaId = activeBanner.Id,
                IsMain = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            },
            new ProductMedia
            {
                Id = Guid.NewGuid(),
                ProductId = deletedProduct.Id,
                MediaId = deletedBanner.Id,
                IsMain = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        return (activeProduct, deletedProduct, category, activeBanner);
    }
}