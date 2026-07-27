using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Address;

public class RemoveAddress: BaseIntegrationTest
{
    public RemoveAddress(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task RemoveAddress_WhenValidData_ShouldReturn200OK()
    {
        var setupResult = await CreateVanillaUserAsync();
        var address = await SetupAddresses(setupResult.User.Id);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);
        var response = await Client.DeleteAsync($"/api/addresses/{address.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    // SUCCESS TEST END
    
    // ERROR TEST START
    
    [Fact]
    public async Task RemoveAddress_WhenUserUnauthorized_ShouldReturn401()
    {
        var setupResult = await CreateVanillaUserAsync();
        var address = await SetupAddresses(setupResult.User.Id);
        var response = await Client.DeleteAsync($"/api/addresses/{address.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    // ERROR TEST END
    
    // SETUP
    private async Task<Domain.Entities.Address> SetupAddresses(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var address1 = new Domain.Entities.Address
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

        dbContext.Add(address1);
        await dbContext.SaveChangesAsync();

        return address1;
    }
}