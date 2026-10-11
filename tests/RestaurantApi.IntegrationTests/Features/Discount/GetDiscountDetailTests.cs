using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Discount;

public class GetDiscountDetailTests : BaseIntegrationTest
{
    public GetDiscountDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetDiscountDetail_WhenDiscountIsActive_ShouldReturnDetailWithPrices()
    {
        var (product, discount) = await Setup(
            startOffsetDays: -2, endOffsetDays: 2, isActive: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{discount.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim detayı başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Id.Should().Be(discount.Id, "Detay dönen indirimin id'si istenen ile aynı olmalıydı.");
        result.Data.ProductId.Should().Be(product.Id);
        result.Data.ProductName.Should().Be(product.Title);
        result.Data.DiscountRate.Should().Be(10d);
        result.Data.Status.Should().Be(DiscountStatus.Active, "Tarihler arasında olduğu için indirim aktif olmalıydı.");
        result.Data.ProductPrice.Should().Be(320m, "Aktif indirimde ürün fiyatı dönmeliydi.");
        result.Data.DiscountedPrice.Should().Be(288m, "İndirimli fiyat (320 - 320*10/100) hesaplanmalıydı.");
    }

    [Fact]
    public async Task GetDiscountDetail_WhenDiscountIsUpcoming_ShouldReturnDetailWithPrices()
    {
        var (_, discount) = await Setup(
            startOffsetDays: 3, endOffsetDays: 6, isActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{discount.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim detayı başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Status.Should().Be(DiscountStatus.Upcoming,
            "Başlangıcı gelecekte olduğu için indirim yakında olmalıydı.");
        result.Data.ProductPrice.Should().Be(320m, "Bitmemiş indirimde ürün fiyatı dönmeliydi.");
        result.Data.DiscountedPrice.Should().Be(288m, "Bitmemiş indirimde indirimli fiyat hesaplanmalıydı.");
    }

    [Fact]
    public async Task GetDiscountDetail_WhenDiscountIsPassive_ShouldReturnNullPrices()
    {
        var (_, discount) = await Setup(
            startOffsetDays: -10, endOffsetDays: -3, isActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{discount.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim detayı başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Status.Should().Be(DiscountStatus.Passive, "Bitiş tarihi geçtiği için indirim pasif olmalıydı.");
        result.Data.ProductPrice.Should().BeNull("İndirim bittiği için ürün fiyatı dönmemeliydi.");
        result.Data.DiscountedPrice.Should().BeNull("İndirim bittiği için indirimli fiyat dönmemeliydi.");
    }

    [Fact]
    public async Task GetDiscountDetail_WhenProductIsDeleted_ShouldReturnDetailWithNullPrices()
    {
        var (product, discount) = await Setup(
            startOffsetDays: -2, endOffsetDays: 2, isActive: true, isProductDeleted: true);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{discount.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Ürün silinmiş olsa da indirim detayı (IgnoreQueryFilters) dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<GetDiscountDetailQueryResult>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.ProductId.Should().Be(product.Id);
        result.Data.ProductName.Should().Be(product.Title, "Ürün silinmiş olsa da adı detayda dönmeliydi.");
        result.Data.Status.Should().Be(DiscountStatus.Active);
        result.Data.ProductPrice.Should().BeNull("Ürün silindiği için ürün fiyatı dönmemeliydi.");
        result.Data.DiscountedPrice.Should().BeNull("Ürün silindiği için indirimli fiyat dönmemeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetDiscountDetail_WhenDiscountNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "İndirim bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Code.Should().Be(Codes.NOT_FOUND, "Response body içerisindeki code NOT_FOUND olmalıydı.");
    }

    [Fact]
    public async Task GetDiscountDetail_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync($"/api/discounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task GetDiscountDetail_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/discounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<(Domain.Entities.Product Product, Domain.Entities.Discount Discount)> Setup(
        int startOffsetDays, int endOffsetDays, bool isActive, bool isProductDeleted = false)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

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
            StartDate = now.AddDays(startOffsetDays),
            EndDate = now.AddDays(endOffsetDays),
            IsActive = isActive
        };

        context.Discounts.Add(discount);
        await context.SaveChangesAsync();

        return (product, discount);
    }
}
