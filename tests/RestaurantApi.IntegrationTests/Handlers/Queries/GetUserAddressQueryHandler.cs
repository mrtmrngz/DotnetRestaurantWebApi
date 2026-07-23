using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Queries;

public class GetUserAddressQueryHandler : BaseIntegrationTest
{
    public GetUserAddressQueryHandler(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TESTS START

    [Fact]
    public async Task GetUserAddressQueryHandler_WhenUserHasAddresses_ShouldReturnAddresses()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id);

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        // act
        var query = new GetUserAddressQuery(UserId: setupResult.User.Id);

        var commandResult = await mediator.Send(query);

        // result check
        commandResult.Should().NotBeNull("Result boş gelmemeliydi.");
        commandResult.Code.Should().Be(Codes.FETH_DATA_SUCCESS, "Result içindeki code FETH_DATA_SUCCESS olmalıydı.");
        commandResult.Data[0].Id.Should().Be(addresses[0].Id,
            "İlk adresin id si oluşturulan ilk adresin id si ile eşleşmeliydi.");
        commandResult.Data[1].Id.Should().Be(addresses[1].Id,
            "İkinci adresin id si oluşturulan ikinci adresin id si ile eşleşmeliydi.");
        commandResult.Data.Count.Should().Be(2, "1 adet silinen adres olduğu için 2 adet adres döndürmesi gerekir.");

        // cache check
        var addressesInCache =
            await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(
                CacheKeys.UserAddressList(setupResult.User.Id.ToString()));

        addressesInCache.Should().NotBeNull("Adreslere redise kaydedilmeliydi.");
        addressesInCache.Count.Should().Be(2, "Rediste kaydedilen adreslerin sayısı 2 olmalıydı.");
    }
    
    [Fact]
    public async Task GetUserAddressQueryHandler_WhenUserHasNotAnyAddresses_ShouldReturnEmptyList()
    {
        var setupResult = await CreateVanillaUserAsync();

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        // act
        var query = new GetUserAddressQuery(UserId: setupResult.User.Id);

        var commandResult = await mediator.Send(query);

        // result check
        commandResult.Should().NotBeNull("Result boş gelmemeliydi.");
        commandResult.Code.Should().Be(Codes.FETH_DATA_SUCCESS, "Result içindeki code FETH_DATA_SUCCESS olmalıydı.");
        commandResult.Data.Should().BeEmpty("Adres olmadığı için data boş liste gelmeliydi.");

        // cache check
        var addressesInCache =
            await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(
                CacheKeys.UserAddressList(setupResult.User.Id.ToString()));

        addressesInCache.Should().BeEmpty("Adreslere redise boş olarak kaydedilmeliydi.");
    }

    // SUCCESS TESTS END
    
    // ERROR TEST START
    
    [Fact]
    public async Task GetUserAddressQueryHandler_WhenUserNotFound_ShouldThrow404()
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // act
        var query = new GetUserAddressQuery(UserId: Guid.NewGuid());

        var act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<NotFoundException>();
    }
    
    // ERROR TEST END
    
    // SETUP ADDRESS
    private async Task<IReadOnlyList<Address>> SetupAddresses(Guid userId)
    {
        var address1 = new Address
        {
            Id = Guid.NewGuid(),
            Title = "Test Address 1",
            RecipientName = "John Doe",
            City = "Test City 1",
            Town = "Test Town 1",
            Neighborhood = "Test Neighborhood 1",
            Street = "Test Street 1",
            BuildingInfo = "Test Building Info",
            BuildingNumber = "Test 1",
            PhoneNumber = "+905441234567",
            IsDefault = true,
            ZipCode = "22100",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UserId = userId,
            IsDeleted = false
        };

        var address2 = new Address
        {
            Id = Guid.NewGuid(),
            Title = "Test Address 2",
            RecipientName = "Jane Doe",
            City = "Test City 2",
            Town = "Test Town 2",
            Neighborhood = "Test Neighborhood 2",
            Street = "Test Street 2",
            BuildingInfo = "Test Building Info",
            BuildingNumber = "Test 2",
            PhoneNumber = "+905441234567",
            IsDefault = false,
            ZipCode = "22100",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UserId = userId,
            IsDeleted = false
        };

        var address3 = new Address
        {
            Id = Guid.NewGuid(),
            Title = "Test Address 3",
            RecipientName = "John Doe",
            City = "Test City 3",
            Town = "Test Town 3",
            Neighborhood = "Test Neighborhood 3",
            Street = "Test Street 3",
            BuildingInfo = "Test Building Info",
            BuildingNumber = "Test 3",
            PhoneNumber = "+905441234567",
            IsDefault = false,
            ZipCode = "22100",
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UserId = userId,
            IsDeleted = true
        };

        var addList = new List<Address> { address1, address2, address3 };

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            dbContext.Addresses.AddRange(addList);

            await dbContext.SaveChangesAsync();
        }

        return addList;
    }
}