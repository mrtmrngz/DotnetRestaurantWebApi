using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Services.Cart;

public class ValidateTargetEntityAsyncTests : BaseIntegrationTest
{
    public ValidateTargetEntityAsyncTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsCurrentUserAndUserExist_ShouldNotThrowNotFoundException()
    {
        var user = await CreateVanillaUserAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync(CartRequestTypes.CurrentUser,
            user.User.Id, tableId: null,
            CancellationToken.None);

        await act.Should()
            .NotThrowAsync(
                "Kullanıcı veri tabanında olduğu için NOT FOUND hatası fırlatmaması gerekirdir.");
    }

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsPanelUserAndRestaurantTableExist_ShouldNotThrowNotFoundException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 4,
            IsActive = true,
            Number = 1
        };

        apiContext.RestaurantTables.Add(table);
        await apiContext.SaveChangesAsync();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync(CartRequestTypes.PanelUser,
            null, tableId: table.Id,
            CancellationToken.None);

        await act.Should()
            .NotThrowAsync(
                "Masa veri tabanında olduğu için NOT FOUND hatası fırlatmaması gerekirdir.");
    }

    // SUCCESS TESTS END


    // ERROR TESTS START

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsCurrentUserAndUserNotExist_ShouldThrowNotFoundException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync(CartRequestTypes.CurrentUser,
            Guid.NewGuid(), tableId: null,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>(
                "Kullanıcı veri tabanında olmadığı için NOT FOUND hatası fırlatması gerekirdi.");
    }

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsPanelUserAndRestaurantTableNotExist_ShouldThrowNotFoundException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync(CartRequestTypes.PanelUser,
            userId: null, tableId: Guid.NewGuid(),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>(
                "Masa veri tabanında olmadığı için NOT FOUND hatası fırlatması gerekirdi.");
    }

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsPanelUserAndRestaurantTableExistButIsNotActive_ShouldThrowNotFoundException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
        var apiContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 4,
            IsActive = false,
            Number = 1
        };

        apiContext.RestaurantTables.Add(table);
        await apiContext.SaveChangesAsync();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync(CartRequestTypes.PanelUser,
            userId: null, tableId: table.Id,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>(
                "Masa veri tabanında olmadığı için NOT FOUND hatası fırlatması gerekirdi.");
    }

    [Theory]
    [InlineData(CartRequestTypes.CurrentUser)]
    [InlineData(CartRequestTypes.PanelUser)]
    public async Task ValidateTargetEntityAsync_WhenRequiredIdIsNull_ShouldThrowBadRequestException(
        CartRequestTypes requestType)
    {
        // Arrange
        await using var scope = Factory.Services.CreateAsyncScope();

        var cartService = scope.ServiceProvider
            .GetRequiredService<ICartService>();

        // Act
        Func<Task> act = () => cartService.ValidateTargetEntityAsync(
            requestType,
            userId: null,
            tableId: null,
            CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }

    [Fact]
    public async Task
        ValidateTargetEntityAsync_WhenRequestTypeIsInvalid_ShouldThrowBadRequestException()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

        Func<Task> act = async () => await cartService.ValidateTargetEntityAsync((CartRequestTypes)999,
            userId: null, tableId: Guid.NewGuid(),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<BadRequestException>(
                "Geçerisiz istek tipi olduğu için BadRequestException fırlatması gerekirdi.")
            .WithMessage("Geçersiz sepet istek tipi.");
    }

    // ERROR TESTS END
}