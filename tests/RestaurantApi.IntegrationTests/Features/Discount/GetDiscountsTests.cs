using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Discount.Queries.GetDiscountListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Enums;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Discount;

public class GetDiscountsTests : BaseIntegrationTest
{
    public GetDiscountsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetDiscounts_WhenNoStatusProvided_ShouldReturnAllDiscounts()
    {
        var (active, passive, upcoming) = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/discounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim listesi başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Should().HaveCount(3, "Status verilmediğinde tüm indirimler dönmeliydi.");
        result.Data.Select(d => d.ProductId).Should().BeEquivalentTo(new[] { active.Id, passive.Id, upcoming.Id },
            "Tüm ürünlerin indirimleri (aktif, pasif, yakında) listelenmeliydi.");
    }

    [Fact]
    public async Task GetDiscounts_WhenStatusIsActive_ShouldReturnOnlyActiveDiscounts()
    {
        var (active, _, _) = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/discounts?status=active");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim listesi başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Should().HaveCount(1, "Yalnızca aktif indirim dönmeliydi.");
        result.Data[0].Status.Should().Be(DiscountStatus.Active);
        result.Data[0].ProductId.Should().Be(active.Id);
        result.Data[0].ProductName.Should().Be(active.Title);
    }

    [Fact]
    public async Task GetDiscounts_WhenStatusIsPassive_ShouldReturnOnlyPassiveDiscounts()
    {
        var (_, passive, _) = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/discounts?status=passive");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim listesi başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Should().HaveCount(1, "Yalnızca biten (pasif) indirim dönmeliydi.");
        result.Data[0].Status.Should().Be(DiscountStatus.Passive);
        result.Data[0].ProductId.Should().Be(passive.Id);
    }

    [Fact]
    public async Task GetDiscounts_WhenStatusIsUpcoming_ShouldReturnOnlyUpcomingDiscounts()
    {
        var (_, _, upcoming) = await Setup();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/discounts?status=upcoming");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İndirim listesi başarılı bir şekilde dönmeliydi.");

        var result = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetDiscountListQueryResult>>>();

        result.Should().NotBeNull("Response body null olmamalıydı.");
        result!.Data.Should().HaveCount(1, "Yalnızca yakında başlayacak indirim dönmeliydi.");
        result.Data[0].Status.Should().Be(DiscountStatus.Upcoming);
        result.Data[0].ProductId.Should().Be(upcoming.Id);
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetDiscounts_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/discounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task GetDiscounts_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/discounts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<(Domain.Entities.Product Active, Domain.Entities.Product Passive, Domain.Entities.Product Upcoming)> Setup()
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

        var activeProduct = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(), Title = "Izgara Tavuk", Slug = "izgara-tavuk",
            Description = "Lezzetli ızgara tavuk", Price = 320, CategoryId = category.Id, IsDeleted = false, CreatedAt = now
        };

        var passiveProduct = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(), Title = "Mercimek Çorbası", Slug = "mercimek-corbasi",
            Description = "Sıcak mercimek çorbası", Price = 95, CategoryId = category.Id, IsDeleted = false, CreatedAt = now
        };

        var upcomingProduct = new Domain.Entities.Product
        {
            Id = Guid.NewGuid(), Title = "Mevsim Salata", Slug = "mevsim-salata",
            Description = "Taze mevsim salata", Price = 120, CategoryId = category.Id, IsDeleted = false, CreatedAt = now
        };

        context.Products.AddRange(activeProduct, passiveProduct, upcomingProduct);

        await context.SaveChangesAsync();

        context.Discounts.AddRange(
            new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(), ProductId = activeProduct.Id, Rate = 10,
                StartDate = now.AddDays(-2), EndDate = now.AddDays(2), IsActive = true
            },
            new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(), ProductId = passiveProduct.Id, Rate = 25,
                StartDate = now.AddDays(-10), EndDate = now.AddDays(-3), IsActive = false
            },
            new Domain.Entities.Discount
            {
                Id = Guid.NewGuid(), ProductId = upcomingProduct.Id, Rate = 15,
                StartDate = now.AddDays(3), EndDate = now.AddDays(6), IsActive = false
            });

        await context.SaveChangesAsync();

        return (activeProduct, passiveProduct, upcomingProduct);
    }
}