using System.Net;
using System.Net.Http.Headers;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Address;

public class GetAddressByIdTests : BaseIntegrationTest
{
    public GetAddressByIdTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TEST START

    [Fact]
    public async Task GetAddressById_WhenAddressExist_ShouldReturnOK()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addressId = await SetupAddresses(setupResult.User.Id);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İstek başarılı olduğu için 200 kodu dönmesi gerekirdi.");
    }

    // SUCCESS TEST END
    
    // ERROR TEST START

    [Fact]
    public async Task GetAddressById_WhenUserNotAuthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync($"/api/addresses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Kullanıcı yetkili olmadığı için 401 hata kodu dönmeliydi.");
    }

    // ERROR TEST END

    // SETUP
    private async Task<Guid> SetupAddresses(Guid userId)
    {
        var address = new Domain.Entities.Address
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

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync();
        }

        return address.Id;
    }
}