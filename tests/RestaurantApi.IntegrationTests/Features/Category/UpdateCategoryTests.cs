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

namespace RestaurantApi.IntegrationTests.Features.Category;

public class UpdateCategoryTests: BaseIntegrationTest
{
    public UpdateCategoryTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task UpdateCategory_WhenTitleUpdated_ShouldReturn200OKAndRemoveCategoriesFromRedis()
    {
        var (categoryEntity, mediaEntity) = await SetupTest();
        var setupResult = await CreateAdminUserAsync();
        
        using var formData = new MultipartFormDataContent();
        
        formData.Add(new StringContent("Updated Title"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/categories/{categoryEntity.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Kategori başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            
            // check media
            var media = await dbContext.Media.FirstOrDefaultAsync(m => m.Id == mediaEntity.Id);
            media.Should().NotBeNull("Media null gelmemeliydi.");
            media.Url.Should().Be(mediaEntity.Url, "Kategori resmi güncellenmediği için eski url ile eşleşmeliydi.");
            
            // check category title
            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryEntity.Id);
            category.Should().NotBeNull("Kategori null gelmemeliydi");
            category.Title.Should().Be("Updated Title", "Kategori başlığı güncellenen başlık ile değişmeliydi.");
            category.Slug.Should().Be("updated-title", "Kategori slug başlık güncellendiği için değişmeliydi.");
            
            // cache clear validation
            var categoriesOnCache = await cache.GetAsync<string>(CacheKeys.Categories());
            categoriesOnCache.Should().BeNull("Kategori güncellendiği için redis categorileri silinmeliydi.");
        }

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi");
        body.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Kategori güncelleme işlemi sonucunda response body içerisindeki code CONTENT_UPDATED_SUCCESS gelmeliydi.");
    }
    
    [Fact]
    public async Task UpdateCategory_WhenImageUpdated_ShouldReturn200OKAndRemoveCategoriesFromRedis()
    {
        var (categoryEntity, mediaEntity) = await SetupTest();
        var setupResult = await CreateAdminUserAsync();
        
        using var formData = new MultipartFormDataContent();
        
        var imageBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
        );
        
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    
        formData.Add(imageContent, "Image", "test.png"); 

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/categories/{categoryEntity.Id}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Kategori başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            
            // check media
            var media = await dbContext.Media.FirstOrDefaultAsync(m => m.Id == mediaEntity.Id);
            media.Should().NotBeNull("Media null gelmemeliydi.");
            media.Url.Should().NotBe(mediaEntity.Url, "Kategori resmi güncellendiği için yeni url ile eski url eşleşmemeliydi.");
            
            // check category title
            var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryEntity.Id);
            category.Should().NotBeNull("Kategori null gelmemeliydi");
            category.Title.Should().Be(categoryEntity.Title, "Kategori başlığı değişmediği için eski başlık ile eşleşmeliydi.");
            category.Slug.Should().Be(categoryEntity.Slug, "Kategori başlığı değişmediği için slug değişmemeliydi.");
            
            // cache clear validation
            var categoriesOnCache = await cache.GetAsync<string>(CacheKeys.Categories());
            categoriesOnCache.Should().BeNull("Kategori güncellendiği için redis categorileri silinmeliydi.");
        }

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi");
        body.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Kategori güncelleme işlemi sonucunda response body içerisindeki code CONTENT_UPDATED_SUCCESS gelmeliydi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task UpdateCategory_WhenUserNotLoggedIn_ShouldReturn401Unauthorized()
    {
        using var formData = new MultipartFormDataContent();
        
        formData.Add(new StringContent("Updated Title"), "Title");
        
        var response = await Client.PatchAsync($"/api/categories/{Guid.NewGuid()}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için 401 UNAUTHORIZED durum kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task UpdateCategory_WhenUserNotAuthorized_ShouldReturn403Forbidden()
    {
        var setupResult = await CreateVanillaUserAsync();
        using var formData = new MultipartFormDataContent();
        
        formData.Add(new StringContent("Updated Title"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);
        
        var response = await Client.PatchAsync($"/api/categories/{Guid.NewGuid()}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Kullanıcı bu işlem için yetkili olmadığı için 403 FORBIDDEN durum kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task UpdateCategory_WhenValidationErrorsAcquired_ShouldReturn400BadRequest()
    {
        var setupResult = await CreateAdminUserAsync();
        
        using var formData = new MultipartFormDataContent();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/categories/{Guid.NewGuid()}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            "Validasyon hataları olduğu için 400 BAD REQUEST durum kodu dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null gelmemeliydi");
        body.Code.Should().Be(Codes.VALIDATION_ERROR,
            "Validasyon hataları olduğu için response body içerisinde VALIDATION_ERROR kodu gelmeliydi.");
    }
    
    [Fact]
    public async Task UpdateCategory_WhenCategoryNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();
        
        using var formData = new MultipartFormDataContent();
        
        formData.Add(new StringContent("Updated Title"), "Title");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/categories/{Guid.NewGuid()}", formData);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Kategori bulunmadığı için NOT_FOUND durum kodu gönderilmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null gelmemeliydi");
        body.Code.Should().Be(Codes.NOT_FOUND,
            "Kategori bulunmadığı için NOT_FOUND kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    
    // SETUP CATEGORY

    private async Task<(Domain.Entities.Category Category, Media Media)> SetupTest()
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
            IsDeleted = false
        };

        dbContext.Categories.Add(category);

        await dbContext.SaveChangesAsync();

        await cache.SetAsync(CacheKeys.Categories(), Guid.NewGuid().ToString(), TimeSpan.FromHours(1));

        return (category, media);
    }
}