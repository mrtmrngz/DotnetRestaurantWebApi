using System.Net;
using System.Net.Http.Headers;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressByIdQuery;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Queries;

public class AddressSetupResultDto
{
    public IReadOnlyList<Address> DbAddresses { get; set; } = null!;
    public IReadOnlyList<GetUserAddressQueryResult> AddressesForRedis { get; set; } = null!;
}

public class GetUserAddressByIdQueryHandlerTest : BaseIntegrationTest
{
    public GetUserAddressByIdQueryHandlerTest(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenAddressNotExistInRedisButExistInDb_ShouldReturnAddressFromDb()
    {
        var userSetupResult = await CreateVanillaUserAsync();
        var addressSetup = await SetupAddresses(userSetupResult.User.Id);

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var query = new GetUserAddressByIdQuery(UserId: userSetupResult.User.Id,
            AddressId: addressSetup.DbAddresses[0].Id);

        var response = await mediator.Send(query);

        response.Should().NotBeNull("Adres ve kullanıcı bulunduğu için response null gelmemeli.");
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS, "Response içerisindeki code FETH_DATA_SUCCESS olmalı");

        response.Data.Id.Should().Be(addressSetup.DbAddresses[0].Id);

        var cacheAfterQuery =
            await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(
                CacheKeys.UserAddressList(userSetupResult.User.Id.ToString()));
        cacheAfterQuery.Should().BeNull("Handler bu işlemde cache warm-up yapmadığı için Redis hala boş olmalı.");
    }
    
    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenAddressExistInRedis_ShouldReturnAddressFromRedis()
    {
        var userSetupResult = await CreateVanillaUserAsync();
        var addressSetup = await SetupAddresses(userSetupResult.User.Id);

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var cacheKey = CacheKeys.UserAddressList(userSetupResult.User.Id.ToString());

        await cacheService.SetAsync(cacheKey, addressSetup.AddressesForRedis, TimeSpan.FromHours(1));

        var query = new GetUserAddressByIdQuery(UserId: userSetupResult.User.Id,
            AddressId: addressSetup.DbAddresses[0].Id);

        var response = await mediator.Send(query);

        response.Should().NotBeNull("Adres ve kullanıcı bulunduğu için response null gelmemeli.");
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS, "Response içerisindeki code FETH_DATA_SUCCESS olmalı");

        response.Data.Id.Should().Be(addressSetup.DbAddresses[0].Id);

        var cacheAfterQuery =
            await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(cacheKey);
        cacheAfterQuery.Should().NotBeNull("Handler bu işlemde cache warm-up yaptığı için veriler redisten dönmeli.");
    }

    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenUserNotExist_ShouldThrow404()
    {
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetUserAddressByIdQuery(UserId: Guid.NewGuid(),
            AddressId: Guid.NewGuid());

        var act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Kullanıcı bulunamadı.");
    }
    
    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenAddressNotExist_ShouldThrow404()
    {
        var usrSetupResult = await CreateVanillaUserAsync();
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetUserAddressByIdQuery(UserId: usrSetupResult.User.Id,
            AddressId: Guid.NewGuid());

        var act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }
    
    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenAddressExistButAddressDeleted_ShouldThrow404()
    {
        var usrSetupResult = await CreateVanillaUserAsync();
        var addressSetupResult = await SetupAddresses(usrSetupResult.User.Id);
        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetUserAddressByIdQuery(UserId: usrSetupResult.User.Id,
            AddressId: addressSetupResult.DbAddresses[1].Id);

        var act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }
    
    [Fact]
    public async Task GetUserAddressByIdQueryHandler_WhenAddressesExistOnCacheButNotExistRequiredAddress_ShouldThrowNotFoundExcepiton()
    {
        var userSetupResult = await CreateVanillaUserAsync();
        var addressSetup = await SetupAddresses(userSetupResult.User.Id);

        using var scope = Factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var cacheKey = CacheKeys.UserAddressList(userSetupResult.User.Id.ToString());

        var cachedAdd = new List<GetUserAddressQueryResult> { addressSetup.AddressesForRedis[1] };

        await cacheService.SetAsync(cacheKey, cachedAdd, TimeSpan.FromHours(1));

        var query = new GetUserAddressByIdQuery(UserId: userSetupResult.User.Id,
            AddressId: addressSetup.DbAddresses[0].Id);

        var act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Adres bulunamadı.");
    }
    
    // ERROR TESTS END

    // SETUP
    private async Task<AddressSetupResultDto> SetupAddresses(Guid userId)
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

        var addList = new List<Address> { address1, address2 };

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        dbContext.Addresses.AddRange(addList);

        await dbContext.SaveChangesAsync();

        var redisAddress = mapper.Map<IReadOnlyList<GetUserAddressQueryResult>>(addList);

        return new AddressSetupResultDto { DbAddresses = addList, AddressesForRedis = redisAddress };
    }
}