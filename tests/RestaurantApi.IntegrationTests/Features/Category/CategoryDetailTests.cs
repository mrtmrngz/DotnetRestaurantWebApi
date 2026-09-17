using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Category.Queries.CategoryDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Category;

public class CategoryDetailTests: BaseIntegrationTest
{
    public CategoryDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TEST START

    [Fact]
    public async Task GetCategoryDetail_WhenCategoryExist_ShouldReturn200()
    {

        var categoryId = Guid.NewGuid();
        
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var media = new Domain.Entities.Media
            {
                Url = "https://localhost:9000/image.png", FileExtension = ".png", FileType = "img/png",
                PublicId = Guid.NewGuid().ToString(), Size = 1024
            };
            
            context.Media.Add(media);
            await context.SaveChangesAsync();
            
            var newCategory = new Domain.Entities.Category
            {
                Id = categoryId,
                Title = "Category 1",
                Slug = "category-1",
                IsDeleted = false,
                MediaId = media.Id
            };
            
            context.Categories.Add(newCategory);

            await context.SaveChangesAsync();
        }

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/categories/{categoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Başarılı durum kodu dönmeliydi.");

        var respBody = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<CategoryDetailQueryResult>>();

        respBody.Should().NotBeNull("Response body null olmamalıydı.");
        respBody.Code.Should()
            .Be(Codes.FETH_DATA_SUCCESS, "Response body içerisindeki code FETH_DATA_SUCCESS olamlıydı");
        respBody.Data.Should().NotBeNull("Response body içerisindeki data null gelmemeliydi.");
        respBody.Data.Id.Should().Be(categoryId,
            "Response body data içerisindeki id oluşturulan category id ile eşleşmeliydi.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task GetCategoryDetail_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için 404 UNAUTHORIZED dönemliydi.");
    }
    
    [Fact]
    public async Task GetCategoryDetail_WhenUserUnauthorizedForThisAction_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();
        var response = await Client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı yetkili olmadığı için 403 FORBIDDEN dönemliydi.");
    }
    
    [Fact]
    public async Task GetCategoryDetail_WhenCategoryNotExist_ShouldReturn404()
    {

        var categoryId = Guid.NewGuid();
       
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/categories/{categoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "Kategori bulunmadığı için 404 NOT_FOUND hata durum kodu dönmeliydi.");

        var respBody = await response.ReadContentAsAsync<BaseResponse>();

        respBody.Should().NotBeNull("Response body null olmamalıydı.");
        respBody.Code.Should()
            .Be(Codes.NOT_FOUND, "Response body içerisindeki code NOT_FOUND olamlıydı");
    }
    
    // ERROR TESTS END
    
}