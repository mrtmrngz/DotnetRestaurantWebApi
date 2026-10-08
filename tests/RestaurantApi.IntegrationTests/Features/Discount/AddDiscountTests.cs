using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Discount;

public class AddDiscountTests : BaseIntegrationTest
{
    public AddDiscountTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task AddDiscount_WhenStartDateIsNow_ShouldCreateActiveDiscountAndClearCache()
    {
        var product = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(3);

        var command = new AddDiscountCommand(product.Id, 15, startDate, endDate);

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "İndirim başarılı bir şekilde oluşturulmalıydı.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS,
            "Oluşturma işlemi sonucunda CONTENT_CREATED_SUCCESS kodu dönmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // db kontrolü
            var discount = await context.Discounts.FirstOrDefaultAsync(d => d.ProductId == product.Id);
            discount.Should().NotBeNull("İndirim veritabanına kaydedilmeliydi.");
            discount!.Rate.Should().Be(15d, "İndirim oranı gönderilen ile aynı olmalıydı.");
            discount.IsActive.Should().BeTrue(
                "Başlangıç tarihi şimdi olduğu için indirim aktif oluşturulmalıydı.");

            // cache invalidation kontrolü
            (await cache.GetAsync<string>(CacheKeys.PublicProducts())).Should().BeNull(
                "İndirim eklendiği için public ürün listesi redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.AdminProducts())).Should().BeNull(
                "İndirim eklendiği için admin ürün listesi redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.PublicProductDetail(product.Slug))).Should().BeNull(
                "İndirim eklendiği için public ürün detayı redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.AdminProductDetail(product.Id))).Should().BeNull(
                "İndirim eklendiği için admin ürün detayı redisten silinmeliydi.");
        }
    }

    [Fact]
    public async Task AddDiscount_WhenStartDateIsInFuture_ShouldCreateInactiveDiscount()
    {
        var product = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(5);

        var command = new AddDiscountCommand(product.Id, 20, startDate, endDate);

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "İndirim başarılı bir şekilde oluşturulmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var discount = await context.Discounts.FirstOrDefaultAsync(d => d.ProductId == product.Id);
            discount.Should().NotBeNull("İndirim veritabanına kaydedilmeliydi.");
            discount!.IsActive.Should().BeFalse(
                "Başlangıç tarihi gelecekte olduğu için indirim pasif oluşturulmalıydı.");
            discount.StartDate.Should().BeCloseTo(startDate, TimeSpan.FromSeconds(1),
                "Başlangıç tarihi gönderilen ile aynı olmalıydı.");
            discount.EndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1),
                "Bitiş tarihi gönderilen ile aynı olmalıydı.");
        }
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task AddDiscount_WhenDiscountRateIsOutOfRange_ShouldReturn400ValidationError()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(Guid.NewGuid(), 0, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "İndirim oranı geçersiz olduğu için 400 dönmeliydi.");

        var result = await response.ReadContentAsAsync<ValidationErrorResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
        result.Errors.Should().Contain(e => e.Field == "DiscountRate", "İndirim oranı hatası dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenProductNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(Guid.NewGuid(), 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Ürün bulunamadığı için 404 dönmeliydi.");

        var result = await response.ReadContentAsAsync<NotFoundException>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenProductAlreadyDeleted_ShouldReturn404()
    {
        var product = await Setup(isDeleted: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(product.Id, 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Ürün silinmiş olduğu için 404 dönmeliydi.");

        var result = await response.ReadContentAsAsync<NotFoundException>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenProductAlreadyHasDiscount_ShouldReturn409()
    {
        var product = await Setup(withDiscount: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(product.Id, 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Ürünün zaten bir indirimi olduğu için 409 CONFLICT dönmeliydi.");

        var result = await response.ReadContentAsAsync<ConflictException>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenStartDateIsInPast_ShouldReturn422()
    {
        var product = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(product.Id, 15, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Başlangıç tarihi geçmiş olduğu için 422 dönmeliydi.");

        var result = await response.ReadContentAsAsync<UnprocessableEntityError>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY, "UNPROCESSABLE_ENTITY kodu dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenUserUnauthorized_ShouldReturn401()
    {
        var command = new AddDiscountCommand(Guid.NewGuid(), 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task AddDiscount_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new AddDiscountCommand(Guid.NewGuid(), 15, DateTime.UtcNow, DateTime.UtcNow.AddDays(1));

        var response = await Client.PostAsJsonAsync("/api/discounts", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<Domain.Entities.Product> Setup(bool isDeleted = false, bool withDiscount = false)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var categoryMedia = new Domain.Entities.Media
        {
            Id = Guid.NewGuid(),
            PublicId = Guid.NewGuid().ToString(),
            Url = "category.png",
            FileExtension = ".png",
            FileType = "image/png",
            Size = 1024
        };

        context.Media.Add(categoryMedia);

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

        var product = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            Title = "Izgara Tavuk",
            Slug = "izgara-tavuk",
            Description = "Lezzetli ızgara tavuk",
            Price = 320,
            CategoryId = category.Id,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        if (withDiscount)
        {
            context.Discounts.Add(new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Rate = 10,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2),
                IsActive = true
            });

            await context.SaveChangesAsync();
        }

        await cache.SetAsync(CacheKeys.PublicProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminProducts(), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.PublicProductDetail(product.Slug), "prd", TimeSpan.FromHours(1));
        await cache.SetAsync(CacheKeys.AdminProductDetail(product.Id), "prd", TimeSpan.FromHours(1));

        return product;
    }
}