using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Infrastructure.Discount;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Discount;

public class DiscountStatusJobTests : BaseIntegrationTest
{
    public DiscountStatusJobTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ActivateAsync_WhenDiscountExists_ShouldSetActiveAndClearCache()
    {
        var (discountId, product) = await Setup(isActive: false);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await cache.SetAsync(CacheKeys.PublicProducts(), "prd", TimeSpan.FromHours(1));
            await cache.SetAsync(CacheKeys.AdminProducts(), "prd", TimeSpan.FromHours(1));
            await cache.SetAsync(CacheKeys.PublicProductDetail(product.Slug), "prd", TimeSpan.FromHours(1));
            await cache.SetAsync(CacheKeys.AdminProductDetail(product.Id), "prd", TimeSpan.FromHours(1));
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<DiscountStatusJob>();

            await job.ActivateAsync(discountId);
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var discount = await context.Discounts.FirstOrDefaultAsync(d => d.Id == discountId);
            discount.Should().NotBeNull("İndirim veritabanında bulunmalıydı.");
            discount!.IsActive.Should().BeTrue("İndirim aktive edilmeliydi.");

            (await cache.GetAsync<string>(CacheKeys.PublicProducts())).Should().BeNull(
                "İndirim durumu değiştiği için public ürün listesi redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.AdminProducts())).Should().BeNull(
                "İndirim durumu değiştiği için admin ürün listesi redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.PublicProductDetail(product.Slug))).Should().BeNull(
                "İndirim durumu değiştiği için public ürün detayı redisten silinmeliydi.");
            (await cache.GetAsync<string>(CacheKeys.AdminProductDetail(product.Id))).Should().BeNull(
                "İndirim durumu değiştiği için admin ürün detayı redisten silinmeliydi.");
        }
    }

    [Fact]
    public async Task DeactivateAsync_WhenDiscountExists_ShouldSetInactive()
    {
        var (discountId, _) = await Setup(isActive: true);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<DiscountStatusJob>();

            await job.DeactivateAsync(discountId);
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var discount = await context.Discounts.FirstOrDefaultAsync(d => d.Id == discountId);
            discount.Should().NotBeNull("İndirim veritabanında bulunmalıydı.");
            discount!.IsActive.Should().BeFalse("İndirim deaktive edilmeliydi.");
        }
    }

    [Fact]
    public async Task ActivateAsync_WhenDiscountNotExist_ShouldNotThrow()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var job = scope.ServiceProvider.GetRequiredService<DiscountStatusJob>();

            var act = async () => await job.ActivateAsync(Guid.NewGuid());

            await act.Should().NotThrowAsync("Olmayan indirim için iş hata vermemeliydi.");
        }
    }

    // SETUP
    private async Task<(Guid DiscountId, Domain.Entities.Product Product)> Setup(bool isActive)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

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
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);

        await context.SaveChangesAsync();

        var discount = new Domain.Entities.Discount
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Rate = 15,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(3),
            IsActive = isActive
        };

        context.Discounts.Add(discount);

        await context.SaveChangesAsync();

        return (discount.Id, product);
    }
}
