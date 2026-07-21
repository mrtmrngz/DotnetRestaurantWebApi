using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Services.Address;

public class CreateAddressAsyncServiceTests: BaseIntegrationTest
{
    public CreateAddressAsyncServiceTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TESTS START

    [Fact]
    public async Task CreateAddressAsyncService_WhenUserHasNoAddress_ShouldSetIsDefaultToTrue()
    {
        var setupResult = await CreateVanillaUserAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var newAddress = new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            UserId = setupResult.User.Id,
            Title = "Ev",
            RecipientName = "John Doe",
            City = "İstanbul",
            BuildingInfo = "Test Bulding",
            BuildingNumber = "13A",
            Street = "Test Street",
            Neighborhood = "Test Neighbor",
            ZipCode = "T2200",
            PhoneNumber = "+905421234567",
            Town = "Test Town",
            IsDefault = false
        };

        var response = await addressService.CreateAddressAsyncService(newAddress, CancellationToken.None);

        response.Should().NotBeNull();
        response.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS);

        var addressInDb = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == newAddress.Id);

        addressInDb.Should().NotBeNull();
        addressInDb.IsDefault.Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateAddressAsyncService_WhenUserAnotherDefaultAndNewAddressIsDefault_ShouldSetIsDefaultToTrue()
    {
        var setupResult = await CreateVanillaUserAsync();

        await using var scope = Factory.Services.CreateAsyncScope();
        var addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var oldAddress = new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            UserId = setupResult.User.Id,
            Title = "Ev",
            RecipientName = "John Doe",
            City = "İstanbul",
            BuildingInfo = "Test Bulding",
            BuildingNumber = "13A",
            Street = "Test Street",
            Neighborhood = "Test Neighbor",
            ZipCode = "T2200",
            PhoneNumber = "+905421234567",
            Town = "Test Town",
            IsDefault = true
        };

        dbContext.Addresses.Add(oldAddress);

        await dbContext.SaveChangesAsync();
        
        var newAddress = new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            UserId = setupResult.User.Id,
            Title = "Ev",
            RecipientName = "John Doe",
            City = "İstanbul",
            BuildingInfo = "Test Bulding",
            BuildingNumber = "13A",
            Street = "Test Street",
            Neighborhood = "Test Neighbor",
            ZipCode = "T2200",
            PhoneNumber = "+905421234567",
            Town = "Test Town",
            IsDefault = true
        };

        var response = await addressService.CreateAddressAsyncService(newAddress, CancellationToken.None);
        
        dbContext.ChangeTracker.Clear();

        response.Should().NotBeNull();
        response.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS);

        var newAddressInDb = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == newAddress.Id);
        var oldAddressInDb = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == oldAddress.Id);

        newAddressInDb.Should().NotBeNull("Yeni adres boş olmamalı.");
        oldAddressInDb.Should().NotBeNull("Eski adres boş olmamalı.");
        newAddressInDb.IsDefault.Should().BeTrue("Yeni adres varsayılan adres olmalı.");
        oldAddressInDb.IsDefault.Should().BeFalse("Eski adres varsayılan adres durumu false olmalı.");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Fact]
    public async Task CreateAddressAsyncService_WhenUserNotExist_ShouldThrow404()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var addressService = scope.ServiceProvider.GetRequiredService<IAddressService>();
        
        var newAddress = new Domain.Entities.Address
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Ev",
            RecipientName = "John Doe",
            City = "İstanbul",
            BuildingInfo = "Test Bulding",
            BuildingNumber = "13A",
            Street = "Test Street",
            Neighborhood = "Test Neighbor",
            ZipCode = "T2200",
            PhoneNumber = "+905421234567",
            Town = "Test Town",
            IsDefault = false
        };

        Func<Task> act = async () => await addressService.CreateAddressAsyncService(newAddress, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("Veri tabanında ilgili kullanıcı yoksa 404 fırlatmalı.")
            .WithMessage("Kullanıcı bulunamadı.");
    }
    
    // ERROR TESTS END
}