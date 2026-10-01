using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Products;

public class UpdateProductTests : BaseIntegrationTest
{
    public UpdateProductTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task UpdateProduct_WhenProductFieldsUpdated_ShouldPersistChangesAndInvalidateCaches()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Updated Title"), "Title");
        formData.Add(new StringContent("Updated Description"), "Description");
        formData.Add(new StringContent((1999).ToString(CultureInfo.InvariantCulture)), "Price");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var updatedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            updatedProduct.Should().NotBeNull("Ürün null gelmemeliydi.");
            updatedProduct!.Title.Should().Be("Updated Title", "Ürün başlığı güncellenmeliydi.");
            updatedProduct.Description.Should().Be("Updated Description", "Ürün açıklaması güncellenmeliydi.");
            updatedProduct.Price.Should().Be(1999m, "Ürün fiyatı güncellenmeliydi.");
            updatedProduct.Slug.Should().Be("updated-title",
                "Ürün başlığı değiştiği için slug yeniden oluşturulmalıydı.");

            var publicProducts = await cache.GetAsync<string>(CacheKeys.PublicProducts());
            var adminProducts = await cache.GetAsync<string>(CacheKeys.AdminProducts());
            var publicProductDetail = await cache.GetAsync<string>(CacheKeys.PublicProductDetail(product.Id));
            var adminProductDetail = await cache.GetAsync<string>(CacheKeys.AdminProductDetail(product.Id));

            publicProducts.Should().BeNull("Güncelleme sonrası public ürün listesi redisten temizlenmeliydi.");
            adminProducts.Should().BeNull("Güncelleme sonrası admin ürün listesi redisten temizlenmeliydi.");
            publicProductDetail.Should().BeNull("Güncelleme sonrası public ürün detayı redisten temizlenmeliydi.");
            adminProductDetail.Should().BeNull("Güncelleme sonrası admin ürün detayı redisten temizlenmeliydi.");
        }

        var body = await response.ReadContentAsAsync<ProductCreateUpdateResponse>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Güncelleme başarılı olduğu için CONTENT_UPDATED_SUCCESS dönmeliydi.");
        body.Id.Should().Be(product.Id, "Response içerisindeki Id güncellenen ürünün id'si olmalıydı.");
    }

    [Fact]
    public async Task UpdateProduct_WhenTitleNotChanged_ShouldKeepExistingSlug()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(product.Title), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var updatedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            updatedProduct.Should().NotBeNull("Ürün null gelmemeliydi.");
            updatedProduct!.Slug.Should().Be("test-title", "Başlık değişmediği için mevcut slug korunmalıydı.");
        }
    }

    [Fact]
    public async Task UpdateProduct_WhenImagesUploaded_ShouldAddNewProductMedias()
    {
        var (product, _, productMedias) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        AddImagesToForm(formData, 2);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var prdMds = await dbContext.ProductMedias.Where(pm => pm.ProductId == product.Id).ToListAsync();
            prdMds.Should().HaveCount(productMedias.Count + 2,
                "Yüklenen yeni görsel sayısı kadar ürün görseli eklenmeliydi.");

            var newMediaIds = await dbContext.Media
                .Where(m => m.PublicId.StartsWith("media/test_"))
                .Select(m => m.Id)
                .ToListAsync();

            newMediaIds.Should().HaveCount(2, "Yüklenen her görsel için bir media kaydı oluşturulmalıydı.");

            var linkedToProduct = await dbContext.ProductMedias
                .CountAsync(pm => pm.ProductId == product.Id && newMediaIds.Contains(pm.MediaId));

            linkedToProduct.Should().Be(2, "Yeni yüklenen görseller ilgili ürün ile ilişkilendirilmeliydi.");
        }
    }

    [Fact]
    public async Task UpdateProduct_WhenImageDeleted_ShouldRemoveMediaAndReorder()
    {
        var (product, _, productMedias) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("test-1"), "DeletedImagePublicIds");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var deletedMedia = await dbContext.Media.FirstOrDefaultAsync(m => m.PublicId == "test-1");
            deletedMedia.Should().BeNull("Silinen görsele ait media veritabanından kaldırılmalıydı.");

            var prdMds = await dbContext.ProductMedias
                .Where(pm => pm.ProductId == product.Id)
                .OrderBy(pm => pm.SortOrder)
                .ToListAsync();

            prdMds.Should().HaveCount(productMedias.Count - 1, "Silinen görsel sonrası kalan ürün görselleri korunmalıydı.");
            prdMds.Count(pm => pm.IsMain).Should().Be(1, "Kalan görsellerden tam olarak bir tanesi ana görsel olmalıydı.");
            prdMds.Select(pm => pm.SortOrder).Should().BeEquivalentTo(new[] { 0, 1 },
                "Silme sonrası SortOrder değerleri baştan düzenlenmelidir.");
        }
    }

    [Fact]
    public async Task UpdateProduct_WhenImageAddedAndOldImageDeleted_ShouldUpdateImages()
    {
        var (product, _, productMedias) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("test-1"), "DeletedImagePublicIds");
        AddImagesToForm(formData, 2);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var deletedMedia = await dbContext.Media.FirstOrDefaultAsync(m => m.PublicId == "test-1");
            deletedMedia.Should().BeNull("Silinen görsele ait media veritabanından kaldırılmalıydı.");

            var prdMds = await dbContext.ProductMedias.Where(pm => pm.ProductId == product.Id).ToListAsync();

            prdMds.Should().HaveCount(productMedias.Count - 1 + 2,
                "Silinen görsel düşülüp eklenen yeni görseller ile ürün görsel sayısı güncellenmeliydi.");
            prdMds.Count(pm => pm.IsMain).Should().Be(1, "Ürünün tam olarak bir ana görseli olmalıydı.");
        }
    }

    [Fact]
    public async Task UpdateProduct_WhenCategoryUpdated_ShouldChangeCategory()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        Guid newCategoryId;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var newCategory = new Domain.Entities.Category
            {
                Id = Guid.NewGuid(),
                Title = "Yeni Kategori",
                Slug = "yeni-kategori",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(newCategory);
            await dbContext.SaveChangesAsync();

            newCategoryId = newCategory.Id;
        }

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(newCategoryId.ToString()), "CategoryId");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Ürün başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var updatedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            updatedProduct.Should().NotBeNull("Ürün null gelmemeliydi.");
            updatedProduct!.CategoryId.Should().Be(newCategoryId, "Ürün yeni kategoriye taşınmalıydı.");
        }
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task UpdateProduct_WhenTitleTooShort_ShouldReturn400ValidationError()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("A"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Başlık çok kısa olduğu için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "Validasyon hatası olduğu için VALIDATION_ERROR kodu dönmeliydi.");
        body.Errors.Should().Contain(e => e.Field == "Title", "Başlık alanına ait validasyon hatası dönmeliydi.");
    }

    [Fact]
    public async Task UpdateProduct_WhenProductNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Updated Title"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{Guid.NewGuid()}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Ürün bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "Ürün bulunamadığı için NOT_FOUND kodu dönmeliydi.");
        body.Message.Should().Be("Ürün bulunamadı.");
    }

    [Fact]
    public async Task UpdateProduct_WhenCategoryNotExist_ShouldReturn404()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(Guid.NewGuid().ToString()), "CategoryId");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Kategori bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "Kategori bulunamadığı için NOT_FOUND kodu dönmeliydi.");
        body.Message.Should().Be("Kategori bulunamadı.");
    }

    [Fact]
    public async Task UpdateProduct_WhenUserUnauthorized_ShouldReturn401()
    {
        var (product, _, _) = await TestSetup();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Updated Title"), "Title");

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kimlik doğrulaması olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task UpdateProduct_WhenUserNotAdmin_ShouldReturn403()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateVanillaUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Updated Title"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı bu işlem için yetkili olmadığından 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task UpdateProduct_WhenDeletedImageNotBelongToProduct_ShouldReturn400BadRequest()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("belongs-to-another-product"), "DeletedImagePublicIds");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Silinmek istenen görsel ürüne ait olmadığı için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<BadRequestException>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.BAD_REQUEST, "Görsel ürüne ait olmadığı için BAD_REQUEST kodu dönmeliydi.");
        body.Message.Should().Be("Silinmek istenen görseller bu ürüne ait değil.");
    }

    [Fact]
    public async Task UpdateProduct_WhenAllImagesDeletedWithoutNew_ShouldReturn422()
    {
        var (product, publicIds) = await SetupProductWithDistinctMediaAsync(mediaCount: 2);
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        foreach (var publicId in publicIds)
        {
            formData.Add(new StringContent(publicId), "DeletedImagePublicIds");
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Ürün resimsiz kalamayacağından 422 UNPROCESSABLE ENTITY dönmeliydi.");

        var body = await response.ReadContentAsAsync<UnprocessableEntityError>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY,
            "Ürün resimsiz kalamayacağından UNPROCESSABLE_ENTITY kodu dönmeliydi.");
        body.Message.Should().Be("Ürün resimsiz olamaz.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var prdMds = await dbContext.ProductMedias.Where(pm => pm.ProductId == product.Id).ToListAsync();
            prdMds.Should().HaveCount(2, "İşlem reddedildiği için ürün görselleri değişmemeliydi.");
        }
    }

    [Fact]
    public async Task UpdateProduct_WhenImageCountExceedsMaximum_ShouldReturn422()
    {
        var (product, _, _) = await TestSetup();
        var setupResult = await CreateAdminUserAsync();

        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("test-1"), "DeletedImagePublicIds");
        AddImagesToForm(formData, 10);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/products/{product.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Görsel sayısı limiti aşılacağından 422 UNPROCESSABLE ENTITY dönmeliydi.");

        var body = await response.ReadContentAsAsync<UnprocessableEntityError>();
        body.Should().NotBeNull("Response body null gelmemeliydi.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY,
            "Görsel limiti aşıldığından UNPROCESSABLE_ENTITY kodu dönmeliydi.");
        body.Message.Should().Be("Bir üründe en fazla 10 adet resim olabilir.");
    }

    // ERROR TESTS END

    // SETUP
    public async Task<(Product prd, Guid catId, IReadOnlyList<ProductMedia> productMedias)> TestSetup()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        Media catMedia = new Media
        {
            PublicId = "test",
            Url = "test",
            FileExtension = "ts",
            FileType = "ts",
            Size = 1024
        };

        context.Media.Add(catMedia);
        await context.SaveChangesAsync();

        Domain.Entities.Category cat = new Domain.Entities.Category
        {
            Title = "Cat",
            MediaId = catMedia.Id,
            Slug = "cat",
            IsDeleted = false
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        Product prd = new Product
        {
            Title = "Test Title",
            Price = 2300,
            AvgRate = 0,
            CommentCount = 0,
            Description = "Test Desc",
            CategoryId = cat.Id,
            Slug = "test-title",
            IsDeleted = false,
            TotalSold = 10,
        };

        context.Products.Add(prd);
        await context.SaveChangesAsync();

        List<Media> medias = new List<Media>
        {
            new Media
            {
                PublicId = "test-1",
                Url = "test-1",
                FileExtension = "ts",
                FileType = "ts",
                Size = 1024
            },
            new Media
            {
                PublicId = "test-2",
                Url = "test-2",
                FileExtension = "ts",
                FileType = "ts",
                Size = 1024
            },
            new Media
            {
                PublicId = "test-3",
                Url = "test-3",
                FileExtension = "ts",
                FileType = "ts",
                Size = 1024
            },
        };

        context.Media.AddRange(medias);
        await context.SaveChangesAsync();

        List<ProductMedia> prdm = medias.Select((media, index) => new ProductMedia
        {
            MediaId = media.Id,
            IsMain = index == 0,
            ProductId = prd.Id,
            SortOrder = index
        }).ToList();

        context.ProductMedias.AddRange(prdm);
        await context.SaveChangesAsync();

        await cache.SetAsync(CacheKeys.AdminProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.PublicProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminProductDetail(prd.Id), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.PublicProductDetail(prd.Id), "prd", TimeSpan.FromHours(1));

        return (prd, cat.Id, prdm);
    }

    private async Task<(Product Product, List<string> PublicIds)> SetupProductWithDistinctMediaAsync(int mediaCount)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var category = new Domain.Entities.Category
        {
            Id = Guid.NewGuid(),
            Title = "Distinct Category",
            Slug = "distinct-category",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = "Distinct Product",
            Slug = "distinct-product",
            Description = "Distinct Desc",
            Price = 100,
            CategoryId = category.Id,
            IsDeleted = false,
            TotalSold = 0
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var publicIds = new List<string>();

        for (int i = 0; i < mediaCount; i++)
        {
            var publicId = $"distinct-media-{i}";

            var media = new Media
            {
                Id = Guid.NewGuid(),
                PublicId = publicId,
                Url = publicId,
                FileExtension = ".png",
                FileType = "image/png",
                Size = 1024
            };

            context.Media.Add(media);
            await context.SaveChangesAsync();

            context.ProductMedias.Add(new ProductMedia
            {
                Id = Guid.NewGuid(),
                MediaId = media.Id,
                ProductId = product.Id,
                IsMain = i == 0,
                SortOrder = i
            });
            await context.SaveChangesAsync();

            publicIds.Add(publicId);
        }

        return (product, publicIds);
    }

    private static readonly byte[] ValidPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
    );

    private static void AddImagesToForm(MultipartFormDataContent formData, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var imageContent = new ByteArrayContent(ValidPngBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(imageContent, "Images", $"test{i}.png");
        }
    }
}