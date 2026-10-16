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
using RestaurantApi.Application.Features.Discount.Commands.UpdateDiscountCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Discount;

public class UpdateDiscountTests: BaseIntegrationTest
{
    public UpdateDiscountTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task UpdateDiscount_WhenRateUpdated_ShouldReturn200AndClearCaches()
    {
        var (discount, product, _) = await SetupTest();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(Rate: 35);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim başarılı bir şekilde güncellenmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Güncelleme işlemi sonucunda CONTENT_UPDATED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedDiscount = await context.Discounts.FirstOrDefaultAsync(d => d.Id == discount.Id);

        updatedDiscount.Should().NotBeNull("İndirim veritabanında bulunmalıydı.");
        updatedDiscount!.Rate.Should().Be(35d, "İndirim oranı gönderilen oran ile güncellenmeliydi.");

        await AssertCachesClearedAsync(cache, product);
    }

    [Fact]
    public async Task UpdateDiscount_WhenEndDateUpdated_ShouldReturn200AndUpdateEndDate()
    {
        var (discount, product, _) = await SetupTest();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var newEndDate = DateTime.UtcNow.AddDays(10);

        var command = new UpdateDiscountCommand(EndDate: newEndDate);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim başarılı bir şekilde güncellenmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Güncelleme işlemi sonucunda CONTENT_UPDATED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedDiscount = await context.Discounts.FirstOrDefaultAsync(d => d.Id == discount.Id);

        updatedDiscount.Should().NotBeNull("İndirim veritabanında bulunmalıydı.");
        updatedDiscount!.EndDate.Should().BeCloseTo(newEndDate, TimeSpan.FromSeconds(1),
            "İndirim bitiş tarihi gönderilen tarih ile güncellenmeliydi.");

        await AssertCachesClearedAsync(cache, product);
    }

    [Fact]
    public async Task UpdateDiscount_WhenIsActiveUpdated_ShouldReturn200AndUpdateIsActive()
    {
        var now = DateTime.UtcNow;

        var (discount, product, _) = await SetupTest(
            discountStartDate: now.AddDays(2), discountEndDate: now.AddDays(5), discountIsActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(IsActive: true);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim başarılı bir şekilde güncellenmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Güncelleme işlemi sonucunda CONTENT_UPDATED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedDiscount = await context.Discounts.FirstOrDefaultAsync(d => d.Id == discount.Id);

        updatedDiscount.Should().NotBeNull("İndirim veritabanında bulunmalıydı.");
        updatedDiscount!.IsActive.Should().BeTrue("İndirim isActive alanı gönderilen değer ile güncellenmeliydi.");

        await AssertCachesClearedAsync(cache, product);
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task UpdateDiscount_WhenNoFieldProvided_ShouldReturn400()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand();

        var response = await Client.PatchAsync($"/api/discounts/{Guid.NewGuid()}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Güncellenecek alan gönderilmediği için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenUserUnauthorized_ShouldReturn401()
    {
        var command = new UpdateDiscountCommand(Rate: 20);

        var response = await Client.PatchAsync($"/api/discounts/{Guid.NewGuid()}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(Rate: 20);

        var response = await Client.PatchAsync($"/api/discounts/{Guid.NewGuid()}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenDiscountNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(Rate: 20);

        var response = await Client.PatchAsync($"/api/discounts/{Guid.NewGuid()}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "İndirim bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenDiscountEnded_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var (discount, _, _) = await SetupTest(
            discountStartDate: now.AddDays(-10), discountEndDate: now.AddDays(-3), discountIsActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(EndDate: now.AddDays(10));

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Bitmiş indirim güncellenemeyeceği için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenStartDateUpdatedButDiscountStarted_ShouldReturn409()
    {
        var (discount, _, _) = await SetupTest();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(StartDate: DateTime.UtcNow.AddDays(3));

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Başlamış indirimin başlangıç tarihi güncellenemeyeceği için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenRateUpdatedButDiscountEnded_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var (discount, _, _) = await SetupTest(
            discountStartDate: now.AddDays(-10), discountEndDate: now.AddDays(-3), discountIsActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(Rate: 50);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Bitmiş indirimin oranı güncellenemeyeceği için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenProductIdUpdatedButProductDeleted_ShouldReturn404()
    {
        var now = DateTime.UtcNow;

        var (discount, _, secondProduct) = await SetupTest(
            discountStartDate: now.AddDays(2), discountEndDate: now.AddDays(5), discountIsActive: false,
            withSecondProduct: true, isSecondProductDeleted: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(ProductId: secondProduct!.Id);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Yeni ürün silinmiş olduğu için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenNewProductHasActiveDiscount_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var (discount, _, secondProduct) = await SetupTest(
            discountStartDate: now.AddDays(2), discountEndDate: now.AddDays(5), discountIsActive: false,
            withSecondProduct: true, secondProductHasActiveDiscount: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(ProductId: secondProduct!.Id);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Yeni ürünün zaten aktif bir indirimi olduğu için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateDiscount_WhenIsActiveUpdatedButDiscountEnded_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var (discount, _, _) = await SetupTest(
            discountStartDate: now.AddDays(-10), discountEndDate: now.AddDays(-3), discountIsActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new UpdateDiscountCommand(IsActive: true);

        var response = await Client.PatchAsync($"/api/discounts/{discount.Id}", JsonContent.Create(command));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Bitmiş indirimin isActive alanı güncellenemeyeceği için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<(Domain.Entities.Discount Discount, Domain.Entities.Product Product,
        Domain.Entities.Product? SecondProduct)> SetupTest(
        DateTime? discountStartDate = null,
        DateTime? discountEndDate = null,
        bool discountIsActive = true,
        bool isProductDeleted = false,
        bool withSecondProduct = false,
        bool isSecondProductDeleted = false,
        bool secondProductHasActiveDiscount = false)
    {
        using var scope = Factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var now = DateTime.UtcNow;

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
            CreatedAt = now
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
            IsDeleted = isProductDeleted,
            CreatedAt = now
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var discount = new Domain.Entities.Discount
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Rate = 10,
            StartDate = discountStartDate ?? now.AddDays(-2),
            EndDate = discountEndDate ?? now.AddDays(2),
            IsActive = discountIsActive
        };

        context.Discounts.Add(discount);

        Domain.Entities.Product? secondProduct = null;

        if (withSecondProduct)
        {
            secondProduct = new Domain.Entities.Product
            {
                Id = Guid.NewGuid(),
                Title = "Mercimek Çorbası",
                Slug = "mercimek-corbasi",
                Description = "Sıcak mercimek çorbası",
                Price = 95,
                CategoryId = category.Id,
                IsDeleted = isSecondProductDeleted,
                CreatedAt = now
            };

            context.Products.Add(secondProduct);
            await context.SaveChangesAsync();

            if (secondProductHasActiveDiscount)
            {
                context.Discounts.Add(new Domain.Entities.Discount
                {
                    Id = Guid.NewGuid(),
                    ProductId = secondProduct.Id,
                    Rate = 20,
                    StartDate = now.AddDays(-1),
                    EndDate = now.AddDays(3),
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();

        await SeedCachesAsync(cache, product);

        if (secondProduct is not null)
            await SeedCachesAsync(cache, secondProduct);

        return (discount, product, secondProduct);
    }

    private static async Task SeedCachesAsync(ICacheService cache, params Domain.Entities.Product[] products)
    {
        var expiration = TimeSpan.FromHours(1);

        await cache.SetAsync(CacheKeys.PublicProducts(), "products", expiration);
        await cache.SetAsync(CacheKeys.AdminProducts(), "admin-products", expiration);
        await cache.SetAsync(CacheKeys.AdminDiscounts(), "admin-discounts", expiration);

        foreach (var product in products)
        {
            await cache.SetAsync(CacheKeys.PublicProductDetail(product.Slug), "product-detail", expiration);
            await cache.SetAsync(CacheKeys.AdminProductDetail(product.Id), "admin-product-detail", expiration);
        }
    }

    private static async Task AssertCachesClearedAsync(ICacheService cache, params Domain.Entities.Product[] products)
    {
        (await cache.GetAsync<string>(CacheKeys.PublicProducts())).Should().BeNull(
            "İndirim güncellendiği için public ürün listesi redisten silinmeliydi.");
        (await cache.GetAsync<string>(CacheKeys.AdminProducts())).Should().BeNull(
            "İndirim güncellendiği için admin ürün listesi redisten silinmeliydi.");
        (await cache.GetAsync<string>(CacheKeys.AdminDiscounts())).Should().BeNull(
            "İndirim güncellendiği için admin indirim listesi redisten silinmeliydi.");

        foreach (var product in products)
        {
            (await cache.GetAsync<string>(CacheKeys.PublicProductDetail(product.Slug))).Should().BeNull(
                "İndirim güncellendiği için public ürün detayı redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.AdminProductDetail(product.Id))).Should().BeNull(
                "İndirim güncellendiği için admin ürün detayı redisten silinmeliydi.");
        }
    }
}
