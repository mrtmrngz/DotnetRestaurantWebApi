using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Category;

public class CreateCategoryTests: BaseIntegrationTest
{
    public CreateCategoryTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCategory_WhenValidData_ShouldReturn201()
    {
        var setupResult = await CreateAdminUserAsync();
    
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
    
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("CategoryTest"), "Title"); 
        
        using (var scope = Factory.Services.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await redis.SetAsync(CacheKeys.Categories(), "data", TimeSpan.FromHours(1));
        }

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png"); 

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsync("/api/categories", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "Kategori oluşturma işlemi başarılı olduğu için 201 kodu dönmeliydi.");

        var results = await response.ReadContentAsAsync<BaseResponse>();

        results.Should().NotBeNull("Response body null gelmemeli.");
        results.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS,
            "Response body içerisindeki code CONTENT_CREATED_SUCCESS gelmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();
            
            var category = await context.Categories.FirstOrDefaultAsync(x => x.Title == "CategoryTest");

            category.Should().NotBeNull("Kategori veri tabanına başarılı bir şekilde kaydedilmeliydi.");

            var media = await context.Media.FirstOrDefaultAsync(x => x.Id == category.MediaId);

            media.Should().NotBeNull("Kategori resmi başarılı bir şekilde veri tabanına kaydedilmeliydi.");
            media.FileExtension.Should().Be(".png", "Dosya uzantısı yüklenen fotoğraf ile aynı olmalıydı");

            var cacheCheck = await redis.GetAsync<string>(CacheKeys.Categories());
            cacheCheck.Should().BeNull("Kategori oluşturulduktan sonra redisin temizlenmesi gerekliydi.");
        }
    }
    
    [Fact]
    public async Task CreateCategory_WhenValidDataAndAnotherCategoryExistSameTitle_ShouldReturn201()
    {
        var setupResult = await CreateAdminUserAsync();
    
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
    
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Category"), "Title"); 

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png"); 

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        await Client.PostAsync("/api/categories", formData);
        var response = await Client.PostAsync("/api/categories", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "Kategori oluşturma işlemi başarılı olduğu için 201 kodu dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var categories = await context.Categories.ToListAsync();

            categories.Should().HaveCount(2, "kategori listesinde 2 adet kategori olması bekleniyordu.");

            categories[1].Slug.Should().Be("category-1", "Son oluşturulan kategorinin slug'ı category-1 olmalı.");
        }
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task CreateCategory_WhenValidationErrorAcquired_ShouldThrow400()
    {
        var setupResult = await CreateAdminUserAsync();
    
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
    
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(""), "Title"); 

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png"); 

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsync("/api/categories", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Validasyon hatası olduğu için 400 dönmeliydi.");
    }

    [Fact]
    public async Task CreateCategory_WhenUserUnauthorized_ShouldThrow401()
    {
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
    
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Category"), "Title"); 

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png"); 

        var response = await Client.PostAsync("/api/categories", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Kullanıcı yetkili olmadığı için 401 dönmeliydi.");
    }
    
    [Fact]
    public async Task CreateCategory_WhenUserNotAdmin_ShouldThrow403()
    {
        var setupResult = await CreateVanillaUserAsync();
        
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
    
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent("Category"), "Title"); 

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsync("/api/categories", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Kullanıcı giriş yapmış fakat bu işlem için yetkili olmadığı için 403 dönmeliydi.");
    }
    
    // ERROR TESTS END
}