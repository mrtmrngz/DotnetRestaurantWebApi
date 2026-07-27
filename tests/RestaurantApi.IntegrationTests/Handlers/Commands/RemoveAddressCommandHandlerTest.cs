using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Commands.RemoveAddressCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class RemoveAddressCommandHandlerTest : BaseIntegrationTest
{
    public RemoveAddressCommandHandlerTest(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task
        RemoveAddressCommandHandler_Should_SoftDeleteAddressAndPromoteNewestAddressToDefault_When_UserHasMultipleAddressesAndDeletesDefaultAddress(
            int targetAddressIndex, int otherAddressIndex)
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id, 2);
        var targetAddressId = addresses[targetAddressIndex].Id;
        var otherAddressId = addresses[otherAddressIndex].Id;

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveAddressCommand() { UserId = setupResult.User.Id, AddressId = targetAddressId };

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeli.");
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Response içerisindeki code silme istediği olduğu için CONTENT_DELETED_SUCCESS olmalı");

        var targetAddress = await dbContext.Addresses.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == targetAddressId);
        var otherAddress = await dbContext.Addresses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == otherAddressId);

        // check target address
        targetAddress.Should().NotBeNull("Silmek istediğimiz adres null gelmemeli.");
        targetAddress.IsDeleted.Should().BeTrue("Adres silinmiş olması gerekir.");

        // check other address
        otherAddress.Should().NotBeNull("Diğer adresim veritabanında bulunması gerekir.");
        otherAddress.IsDefault.Should().BeTrue("Diğer adres tek kaldığı için varsayılan adres olarak ayarlanmalı.");

        var addressesInCache =
            await cacheService.GetAsync<Address>(CacheKeys.UserAddressList(setupResult.User.Id.ToString()));

        addressesInCache.Should().BeNull("Adresler redisten silinmeliydi.");

        var addressCount = await dbContext.Addresses.CountAsync(x => x.UserId == setupResult.User.Id);

        addressCount.Should().Be(1, "Adres silindiği için query filter sayesinde veritabanında tek adres bulunmalıydı");
    }

    [Fact]
    public async Task RemoveAddressCommandHandler_Should_SoftDeleteAddress_When_UserHasOnlyOneAddress()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id, 1);
        var targetAddressId = addresses[0].Id;

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveAddressCommand { UserId = setupResult.User.Id, AddressId = targetAddressId };

        var response = await mediator.Send(command);

        response.Should().NotBeNull();
        response.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS);

        var targetAddress = await dbContext.Addresses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == targetAddressId);

        targetAddress.Should().NotBeNull();
        targetAddress!.IsDeleted.Should().BeTrue("Adres soft-delete yapılmış olmalı.");
        targetAddress.IsDefault.Should().BeFalse("Silinen adresin varsayılanlığı düşürülmeli.");

        var activeAddressCount = await dbContext.Addresses
            .CountAsync(x => x.UserId == setupResult.User.Id);

        activeAddressCount.Should().Be(0, "Kullanıcının tek adresi silindiği için aktif adresi kalmamalı.");

        // 3. Cache uçurulmalı
        var addressesInCache = await cacheService
            .GetAsync<Address>(CacheKeys.UserAddressList(setupResult.User.Id.ToString()));

        addressesInCache.Should().BeNull("Silme işleminden sonra cache temizlenmeli.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START
    
    [Fact]
    public async Task RemoveAddressCommandHandler_WhenUserNotExist_ShouldThrowNotFoundException()
    {
        var targetAddressId = Guid.NewGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveAddressCommand { UserId = Guid.NewGuid(), AddressId = targetAddressId };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>("User bulunamadığı için Not Found hatası fırlatmalıydı.");
    }

    [Fact]
    public async Task RemoveAddressCommandHandler_WhenAddressNotExist_ShouldThrowNotFoundException()
    {
        var setupResult = await CreateVanillaUserAsync();
        var targetAddressId = Guid.NewGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveAddressCommand { UserId = setupResult.User.Id, AddressId = targetAddressId };

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<NotFoundException>("Adresi bulunamadığı için Not Found hatası fırlatmalıydı.");
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