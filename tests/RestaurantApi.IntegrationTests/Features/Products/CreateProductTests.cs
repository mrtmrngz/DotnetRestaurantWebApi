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

public class CreateProductTests : BaseIntegrationTest
{
    public CreateProductTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TEST START

    [Fact]
    public async Task CreateProduct_WhenValidData_ShouldReturn201()
    {
        var categoryId = Guid.NewGuid();
        var userSetup = await CreateAdminUserAsync();
        using var formData = await PrepareTest(categoryId);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsync("/api/products", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Ürün başarılı bir şekilde oluşturulmalıydı.");

        var responseBody = await response.ReadContentAsAsync<ProductCreateUpdateResponse>();
        responseBody.Should().NotBeNull("Response body null gelmemeliydi.");
        responseBody.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS,
            "Oluşturma işlemi dolayısı ile response body içerisindeki kod CONTENT_CREATED_SUCCESS gelmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // check db
            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == responseBody.Id);
            product.Should().NotBeNull("Ürün null gelmemeliydi.");
            product.Price.Should().Be(1900, "Ürün fiyatı oluşturulan ürün fiyatı ile aynı olmalıydı.");
            product.Title.Should().Be("ProductTest", "Ürün başlığı oluşturuklan ürün ile aynı olmalıydı.");

            var prdMds = await context.ProductMedias.Where(pm => pm.ProductId == responseBody.Id).ToListAsync();
            prdMds.Count.Should().Be(4, "Yüklenen resim kadar fotoğraf olmalıydı.");

            // cache check
            var adminPrds = await cache.GetAsync<string>(CacheKeys.AdminProducts());
            var publicPrds = await cache.GetAsync<string>(CacheKeys.PublicProducts());

            adminPrds.Should().BeNull("Admin ürünleri redis üzerinde silinmesi gerekiyordu.");
            publicPrds.Should().BeNull("Public ürünler redis üzerinde silinmesi gerekiyordu.");
        }
    }

    // SUCCESS TEST END

    // ERROR TESTS START
    
    [Fact]
    public async Task CreateProduct_WhenValidationErrorsAcquired_ShouldReturn400()
    {
        var userSetup = await CreateAdminUserAsync();
        
        using var formData = new MultipartFormDataContent();
        
        formData.Add(new StringContent("ProductTest"), "Title");
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsync("/api/products", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Validasyon hataları ortaya çıktığı için 400 BAD REQUEST hata kodu dönmeliydi..");

        var responseBody = await response.ReadContentAsAsync<ValidationErrorResponse>();
        responseBody.Should().NotBeNull("Response body null gelmemeliydi.");
        responseBody.Code.Should().Be(Codes.VALIDATION_ERROR,
            "Validasyon hataları bulunduğu için body içerisindeki kod VALIDATION_ERROR olarak dönmeliydi.");
        
    }

    [Fact]
    public async Task CreateProduct_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var categoryId = Guid.NewGuid();
        using var formData = await PrepareTest(categoryId);

        var response = await Client.PostAsync("/api/products", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için 401 hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task CreateProduct_WhenUserNotAuthorized_ShouldReturn403()
    {
        var categoryId = Guid.NewGuid();
        var userSetup = await CreateVanillaUserAsync();
        using var formData = await PrepareTest(categoryId);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsync("/api/products", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı bu işlem için yetkili olmadığı için 403 FORBIDDEN hata kodu dönmeliydi.");
    }

    [Fact]
    public async Task CreateProduct_WhenCategoryInvalid_ShouldReturn404()
    {
        var categoryId = Guid.NewGuid();
        var userSetup = await CreateAdminUserAsync();
        using var formData = await PrepareTest(categoryId, false);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PostAsync("/api/products", formData);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kategori bulunamadığı için 404 NOT FOUND durum kodu gelmeliydi.");

        var responseBody = await response.ReadContentAsAsync<NotFoundException>();
        responseBody.Should().NotBeNull("Response body null gelmemeliydi.");
        responseBody.Code.Should().Be(Codes.NOT_FOUND,
            "Kategori bulunamadığı için NOT FOUND kodu gelmeliydi.");
        
    }

    // ERROR TESTS END

    // SUCCESS TEST SETUP
    private async Task<MultipartFormDataContent> PrepareTest(Guid categoryId, bool isCatExist = true)
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var media = new Media
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                FileExtension = ".png",
                FileType = "img/png",
                Size = 1024,
                PublicId = Guid.NewGuid().ToString(),
                Url = "mediaurl"
            };

            context.Media.Add(media);

            await context.SaveChangesAsync();

            if (isCatExist)
            {
                context.Categories.Add(
                    new Domain.Entities.Category
                    {
                        Id = categoryId,
                        IsDeleted = false,
                        Slug = "kategori-1",
                        Title = "Kategori 1",
                        MediaId = media.Id
                    }
                );

                await context.SaveChangesAsync();
            }

            await cache.SetAsync(CacheKeys.AdminProducts(), Guid.NewGuid().ToString(), TimeSpan.FromHours(1));
            await cache.SetAsync(CacheKeys.PublicProducts(), Guid.NewGuid().ToString(), TimeSpan.FromHours(1));
        }

        var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("ProductTest"), "Title");
        formData.Add(new StringContent("ProductTest"), "Description");
        formData.Add(new StringContent(categoryId.ToString()), "CategoryId");
        formData.Add(
            new StringContent((1900).ToString(CultureInfo.InvariantCulture)),
            "Price"
        );

        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );

        for (int i = 1; i <= 4; i++)
        {
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(imageContent, "Images", $"test{i}.png");
        }

        return formData;
    }
}