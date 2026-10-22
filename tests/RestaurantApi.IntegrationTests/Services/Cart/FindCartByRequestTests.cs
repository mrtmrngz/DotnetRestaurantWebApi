using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Services.Cart;

public class FindCartByRequestTests: BaseIntegrationTest
{
    public FindCartByRequestTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task FindCartByRequest_WhenCurrentUserRequest_ShouldReturnUserCartId()
    {
        var user = await CreateVanillaUserAsync();
        var cartId = await SeedUserCart(user.User.Id);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var response = await cartService.FindCartByRequest(CartRequestTypes.CurrentUser, user.User.Id, null,
            CancellationToken.None);

        response.Should().NotBeEmpty("Response boş gelmemeliydi");
        response.Should().Be(cartId, "Response kullanıcının sepet id'si ile aynı olmalıydı.");
    }
    
    [Fact]
    public async Task FindCartByRequest_WhenPanelUserRequest_ShouldReturnTableCartId()
    {
        var tableId = Guid.NewGuid();
        var cartId = await SeedTableCart(tableId);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        var response = await cartService.FindCartByRequest(CartRequestTypes.PanelUser, null, tableId,
            CancellationToken.None);

        response.Should().NotBeEmpty("Response boş gelmemeliydi");
        response.Should().Be(cartId, "Response masanın sepet id'si ile aynı olmalıydı.");
    }
    
    [Fact]
    public async Task FindCartByRequest_WhenMultipleCartsExist_ShouldReturnCorrectUserCartId()
    {
        var targetUser = await CreateVanillaUserAsync();
        var otherUser = await CreateVanillaUserAsync();

        var expectedCartId = await SeedUserCart(targetUser.User.Id);
        await SeedUserCart(otherUser.User.Id);

        await using var scope = Factory.Services.CreateAsyncScope();

        var cartService = scope.ServiceProvider
            .GetRequiredService<ICartService>();

        var result = await cartService.FindCartByRequest(
            CartRequestTypes.CurrentUser,
            targetUser.User.Id,
            null,
            CancellationToken.None);

        result.Should().Be(expectedCartId, "Hedef kullanıcının sepeti dönmesi gerekirdi.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task FindCartByRequest_WhenInvalidRequest_ShouldThrowBadRequestException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async() => await cartService.FindCartByRequest((CartRequestTypes)999, null, null,
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Geçersiz işlem tipi.");
    }
    
    [Fact]
    public async Task FindCartByRequest_WhenCartNotFound_ShouldThrowNotFoundException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async() => await cartService.FindCartByRequest(CartRequestTypes.CurrentUser, Guid.NewGuid(), null,
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
    
    // ERROR TESTS END
    
    // SETUP

    private async Task<Guid> SeedUserCart(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        
        Domain.Entities.Cart cart = new Domain.Entities.Cart
        {
            UserId = userId,
            RestaurantTableId = null,
            Id = Guid.NewGuid()
        };

        apiContext.Carts.Add(cart);
        await apiContext.SaveChangesAsync();

        return cart.Id;
    }
    
    private async Task<Guid> SeedTableCart(Guid tableId)
    {
        using var scope = Factory.Services.CreateScope();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        RestaurantTable table = new RestaurantTable
        {
            Id = tableId,
            Capacity = 2,
            IsActive = true,
            Number = 2
        };

        apiContext.RestaurantTables.Add(table);
        
        Domain.Entities.Cart cart = new Domain.Entities.Cart
        {
            UserId = null,
            RestaurantTableId = tableId,
            Id = Guid.NewGuid()
        };

        apiContext.Carts.Add(cart);
        await apiContext.SaveChangesAsync();

        return cart.Id;
    }
    
}