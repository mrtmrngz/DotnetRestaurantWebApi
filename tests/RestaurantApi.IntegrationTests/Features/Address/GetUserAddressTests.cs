using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Address;

public class GetUserAddressTests : BaseIntegrationTest
{
    public GetUserAddressTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TEST START

    [Fact]
    public async Task GetUserAddress_WhenUserAuthorizeAndExist_ShouldReturnOK()
    {
        var setupResult = await CreateVanillaUserAsync();
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
            CreatedAt = DateTime.UtcNow - TimeSpan.FromHours(1),
            UserId = setupResult.User.Id,
            IsDeleted = false
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var addressInCache =
                await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(
                    CacheKeys.UserAddressList(setupResult.User.Id.ToString()));
            addressInCache.Should().BeNull("İlk çağrı yapılmadığı için rediste kullanıcının adres kaydı olmamalıydı.");

            dbContext.Addresses.Add(address);
            await dbContext.SaveChangesAsync();
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var addressInCache =
                await cacheService.GetAsync<IReadOnlyList<GetUserAddressQueryResult>>(
                    CacheKeys.UserAddressList(setupResult.User.Id.ToString()));
            addressInCache.Should().NotBeNull("İlk çağrı yapıldığı için rediste kullanıcının adres kaydı olmalıydı.");
        }
    }

    // SUCCESS TEST END
    
    // ERROR TEST START
    
    [Fact]
    public async Task GetUserAddress_WhenUserUnauthorizeAndExist_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    // ERROR TEST END
}