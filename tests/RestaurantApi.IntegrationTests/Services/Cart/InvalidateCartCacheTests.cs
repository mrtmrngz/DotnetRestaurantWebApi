using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.IntegrationTests.Setup;

namespace RestaurantApi.IntegrationTests.Services.Cart;

public class InvalidateCartCacheTests: BaseIntegrationTest
{
    public InvalidateCartCacheTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START
    
    [Theory]
    [InlineData(CartRequestTypes.CurrentUser)]
    [InlineData(CartRequestTypes.PanelUser)]
    public async Task InvalidateCartCache_WhenRequestValid_ShouldRemoveCacheByRequestType(CartRequestTypes reqType)
    {
        Guid userId = Guid.NewGuid();
        Guid tableId = Guid.NewGuid();

        await Setup(userId, tableId);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cartService.InvalidateCartCache(reqType, userId, tableId);

        var userInCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(userId));
        var tableInCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(tableId));

        if (reqType == CartRequestTypes.CurrentUser)
        {
            userInCache.Should().BeNull("User id, redisten silinmeliydi.");
            tableInCache.Should().NotBeNull("Masa id, redisten silinmemeliydi.");
        }
        
        if (reqType == CartRequestTypes.PanelUser)
        {
            tableInCache.Should().BeNull("Masa id, redisten silinmeliydi.");
            userInCache.Should().NotBeNull("User id, redisten silinmemeliydi.");
        }
        
    }
    
    // SUCCESS TESTS END
    
    // ERROR TEST START

    [Fact]
    public async Task InvalidateCartCache_WhenRequestInvalid_ShouldThrowBadRequestException()
    {
        Guid userId = Guid.NewGuid();
        Guid tableId = Guid.NewGuid();

        await Setup(userId, tableId);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        Func<Task> act = async() => await cartService.InvalidateCartCache((CartRequestTypes)999, userId, tableId);

        var userInCache = await cacheService.GetAsync<string>(CacheKeys.UserCart(userId));
        var tableInCache = await cacheService.GetAsync<string>(CacheKeys.PanelCart(tableId));
        
        tableInCache.Should().NotBeNull("Masa id, redisten silinmemeliydi.");
        userInCache.Should().NotBeNull("User id, redisten silinmemeliydi.");

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Geçersiz işlem tipi.");

    }
    
    // ERROR TEST END
    
    // SETUP
    private async Task Setup(Guid userId, Guid tableId)
    {
        var scope = Factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cacheService.SetAsync(CacheKeys.UserCart(userId), "test value", TimeSpan.FromHours(1));
        await cacheService.SetAsync(CacheKeys.PanelCart(tableId), "test value", TimeSpan.FromHours(1));
    } 
}