using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Products;

public class PublicProductDetail : BaseIntegrationTest
{
    public PublicProductDetail(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task PublicProductDetail_WhenProductExistsInDb_ShouldReturnFromDbAndSetRedis()
    {
        var (product, category, images) = await Setup();

        var response = await Client.GetAsync($"/api/products/public/{product.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün detayı başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<PublicProductDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.FETH_DATA_SUCCESS,
            "Response body içerisindeki code FETH_DATA_SUCCESS olmalıydı.");
        result.Data.Should().NotBeNull("Response body içerisindeki data null gelmemeliydi.");
        result.Data.Id.Should().Be(product.Id, "Ürün id'si veritabanındaki ile aynı olmalıydı.");
        result.Data.Title.Should().Be(product.Title, "Ürün başlığı veritabanındaki ile aynı olmalıydı.");
        result.Data.Slug.Should().Be(product.Slug, "Ürün slug'ı veritabanındaki ile aynı olmalıydı.");
        result.Data.Description.Should().Be(product.Description, "Ürün açıklaması veritabanındaki ile aynı olmalıydı.");
        result.Data.CategoryName.Should().Be(category.Title, "Kategori adı ilişkili kategoriden gelmeliydi.");
        result.Data.Images.Should().HaveCount(images.Count, "Ürünün tüm görselleri dönmeliydi.");
        result.Data.Images.Count(i => i.IsMain).Should().Be(1, "Görsellerden yalnızca bir tanesi ana görsel olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cached = await cacheService
                .GetAsync<PublicProductDetailQueryResult>(CacheKeys.PublicProductDetail(product.Slug));

            cached.Should().NotBeNull("Veritabanından çekilen ürün detayı redise kaydedilmeliydi.");
            cached!.Title.Should().Be(product.Title, "Redisteki ürün veritabanındaki ile aynı olmalıydı.");
        }
    }

    [Fact]
    public async Task PublicProductDetail_WhenProductOnRedis_ShouldReturnFromRedisWithoutDb()
    {
        var cachedResult = new PublicProductDetailQueryResult
        {
            Id = Guid.NewGuid(),
            Title = "Redis Ürün",
            Description = "Redis açıklama",
            Slug = "redis-urun",
            Price = 75,
            CommentCount = 3,
            AvgRate = 4.8,
            CategoryName = "Redis Kategori",
            Images = new List<ProductDetailImageList>
            {
                new ProductDetailImageList
                {
                    Id = Guid.NewGuid(),
                    PublicId = "redis-image",
                    Url = "redis-image.png",
                    SortOrder = 0,
                    IsMain = true
                }
            }
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await cacheService.SetAsync(CacheKeys.PublicProductDetail(cachedResult.Slug), cachedResult,
                TimeSpan.FromHours(1));
        }

        var response = await Client.GetAsync($"/api/products/public/{cachedResult.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün detayı redisten başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<PublicProductDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Title.Should().Be(cachedResult.Title, "Gelen ürün detayı redisteki ile aynı olmalıydı.");
        result.Data.Images.Should().HaveCount(1, "Rediste 1 adet görsel olduğu için 1 görsel dönmeliydi.");

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
    public async Task PublicProductDetail_WhenProductNotExist_ShouldReturn404()
    {
        var response = await Client.GetAsync($"/api/products/public/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Ürün bulunmadığı için 404 NOT FOUND dönmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "Response body içerisindeki code NOT_FOUND olmalıydı.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<(Product Product, Domain.Entities.Category Category, List<Media> Images)> Setup()
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

        var mainImage = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "image-1.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        var secondImage = new Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "image-2.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        context.Media.AddRange(categoryMedia, mainImage, secondImage);

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
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        context.ProductMedias.AddRange(
            new ProductMedia
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MediaId = mainImage.Id,
                IsMain = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            },
            new ProductMedia
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                MediaId = secondImage.Id,
                IsMain = false,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        return (product, category, new List<Media> { mainImage, secondImage });
    }
}