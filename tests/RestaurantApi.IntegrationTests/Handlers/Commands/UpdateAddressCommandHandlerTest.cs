using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class UpdateAddressCommandHandlerTest : BaseIntegrationTest
{
    public UpdateAddressCommandHandlerTest(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TESTS START

    [Fact]
    public async Task
        UpdateAddressCommandHandler_WhenUserExistAnotherDefaultAddress_ShouldSetDefaultFalseToOldAddressAndSetTrueToNewUpdatedAddress()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id, 2);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var command = new UpdateAddressCommand(
                Title: "Updated Address",
                RecipientName: null,
                City: null,
                Town: null,
                Neighborhood: null,
                Street: null,
                BuildingInfo: "Beyaz apartman",
                BuildingNumber: null,
                PhoneNumber: null,
                IsDefault: true,
                ZipCode: null
            ) with
            {
                AddressId = addresses[1].Id, UserId = setupResult.User.Id
            };

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeli");
        response.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Response içerisindeki code CONTENT_UPDATED_SUCCESS olmalı.");

        var oldAddress = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addresses[0].Id);
        var updatedAddress = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addresses[1].Id);

        oldAddress.Should().NotBeNull("Eski adres veritabında mevcut olmalı.");
        updatedAddress.Should().NotBeNull("Yeni adres veritabında mevcut olmalı.");

        oldAddress.IsDefault.Should()
            .BeFalse("Eski adres yeni adres varsayılan olduğu için IsDefault durumu false olmalı.");
        updatedAddress.IsDefault.Should()
            .BeTrue("Günellenen adres varsayılan yapıldığı için IsDefault durumu true olmalı");
        updatedAddress.Title.Should().Be(command.Title,
            "Güncellenen adresin başlığı command içerisindeki başlık ile aynı olmalı.");
        updatedAddress.BuildingInfo.Should().Be(command.BuildingInfo,
            "Güncellenen adresin bina bilgisi command içerisindeki bina bilgisi ile aynı olmalı.");

        var addressesInCache =
            await cacheService.GetAsync<Address>(CacheKeys.UserAddressList(setupResult.User.Id.ToString()));
        addressesInCache.Should().BeNull("Adres güncellendiği için redis invalidate edilmeliydi.");
    }

    [Fact]
    public async Task
        UpdateAddressCommandHandler_WhenCommandIsDefaultFalseButTargetAddressIsDefaultTrue_ShouldSetIsDefaultFalseToTargetAddAndSetDefaultTrueAnotherAddress()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id, 2);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var command = new UpdateAddressCommand(
                Title: null,
                RecipientName: null,
                City: null,
                Town: null,
                Neighborhood: null,
                Street: null,
                BuildingInfo: null,
                BuildingNumber: null,
                PhoneNumber: null,
                IsDefault: false,
                ZipCode: null
            ) with
            {
                AddressId = addresses[0].Id, UserId = setupResult.User.Id
            };

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeli");
        response.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Response içerisindeki code CONTENT_UPDATED_SUCCESS olmalı.");

        var targetAddress = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addresses[0].Id);
        var anotherAdddress = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addresses[1].Id);

        targetAddress.Should().NotBeNull("Target adres null olmamalı.");
        anotherAdddress.Should().NotBeNull("Diğer adres null olmamalı.");

        targetAddress.IsDefault.Should()
            .BeFalse(
                "Güncellemek istediğimiz adresin commandı isDefault false olduğu için veri tabanınada aynısnı yansımlaıydı.");
        anotherAdddress.IsDefault.Should()
            .BeTrue(
                "Güncellemek istediğimiz adresin commandı isDefault false olduğu için diğer adresimizin IsDefault durumu true olmalı.");

        var addressesInCache =
            await cacheService.GetAsync<Address>(CacheKeys.UserAddressList(setupResult.User.Id.ToString()));
        addressesInCache.Should().BeNull("Adres güncellendiği için redis invalidate edilmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task UpdateAddressCommandHandler_WhenUserNotExist_ShouldThrowNotFoundException()
    {
        var mockUserId = Guid.NewGuid();
        var mockAddressId = Guid.NewGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateAddressCommand(
                Title: null,
                RecipientName: null,
                City: null,
                Town: null,
                Neighborhood: null,
                Street: null,
                BuildingInfo: null,
                BuildingNumber: null,
                PhoneNumber: null,
                IsDefault: false,
                ZipCode: null
            ) with
            {
                AddressId = mockAddressId, UserId = mockUserId
            };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>("Kullanıcı olmadı için Not Found Exception fırlatılmalıydı");
    }

    [Fact]
    public async Task UpdateAddressCommandHandler_WhenAddressNotExist_ShouldThrowNotFoundException()
    {
        var setupResult = await CreateVanillaUserAsync();
        var mockAddressId = Guid.NewGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateAddressCommand(
                Title: null,
                RecipientName: null,
                City: null,
                Town: null,
                Neighborhood: null,
                Street: null,
                BuildingInfo: null,
                BuildingNumber: null,
                PhoneNumber: null,
                IsDefault: false,
                ZipCode: null
            ) with
            {
                AddressId = mockAddressId, UserId = setupResult.User.Id
            };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>("Adres olmadı için Not Found Exception fırlatılmalıydı");
    }

    [Fact]
    public async Task
        UpdateAddressCommandHandler_WhenUserHasOneAddressAndCommandIsDefaultIsFalse_ShouldThrowUnprocessableEntityError()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateAddressCommand(
                Title: null,
                RecipientName: null,
                City: null,
                Town: null,
                Neighborhood: null,
                Street: null,
                BuildingInfo: null,
                BuildingNumber: null,
                PhoneNumber: null,
                IsDefault: false,
                ZipCode: null
            ) with
            {
                AddressId = addresses[0].Id, UserId = setupResult.User.Id
            };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<UnprocessableEntityError>(
            "Hedef adres dışında kullanıcının başka adresi olmadığı için o adresi varsayılandan kaldırmaya izin vermemli bunun sonucunda Unproccesable Entity hatası fırlatmalıydı.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<IReadOnlyList<Address>> SetupAddresses(Guid userId, int addressCount = 1,
        Action<Address>? customConfig = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

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

        customConfig?.Invoke(address1);

        var addressList = new List<Address> { address1 };

        if (addressCount > 1)
        {
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

            addressList.Add(address2);
        }

        dbContext.AddRange(addressList);
        await dbContext.SaveChangesAsync();

        await cacheService.SetAsync(CacheKeys.UserAddressList(userId.ToString()), addressList,
            TimeSpan.FromHours(1));

        return addressList;
    }
}