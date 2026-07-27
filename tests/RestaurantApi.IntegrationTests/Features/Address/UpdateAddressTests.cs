using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Address;

public class UpdateAddressTests : BaseIntegrationTest
{
    public UpdateAddressTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TEST START

    [Fact]
    public async Task UpdateAddress_WhenValidData_ShouldReturn200OK()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id);

        var command = new UpdateAddressCommand(
            Title: "Updated Address",
            RecipientName: "Updated rec",
            City: null,
            Town: null,
            Neighborhood: null,
            Street: "Test street",
            BuildingInfo: "Beyaz apartman",
            BuildingNumber: null,
            PhoneNumber: "5444444444",
            IsDefault: true,
            ZipCode: "22100"
        );

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsJsonAsync($"/api/addresses/{addresses[1].Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task UpdateAddress_WhenUserNotAuthorized_ShouldReturn401UNAUTHORIZED()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id);

        var command = new UpdateAddressCommand(
            Title: "Updated Address",
            RecipientName: "Updated rec",
            City: null,
            Town: null,
            Neighborhood: null,
            Street: "Test street",
            BuildingInfo: "Beyaz apartman",
            BuildingNumber: null,
            PhoneNumber: "5444444444",
            IsDefault: true,
            ZipCode: "22100"
        );

        var response = await Client.PatchAsJsonAsync($"/api/addresses/{addresses[1].Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Kullanıcı yetkili olmadığı için 401 dönmeli.");
    }

    [Fact]
    public async Task UpdateAddress_WhenAllFieldsAreNull_ShouldReturn400ValidationError()
    {
        var setupResult = await CreateVanillaUserAsync();
        var addresses = await SetupAddresses(setupResult.User.Id);

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
            IsDefault: null,
            ZipCode: null
        );

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsJsonAsync($"/api/addresses/{addresses[1].Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadContentAsAsync<BaseResponse>();
        result.Should().NotBeNull("Response body null gelmemeli.");
        result.Code.Should().Be(Codes.VALIDATION_ERROR,
            "Tüm alanlar boş geldiği için response body'deki code VALIDATION_ERROR olmalı.");
    }

    // ERROR TESTS END


    // SETUP
    private async Task<IReadOnlyList<Domain.Entities.Address>> SetupAddresses(Guid userId)
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

        var address2 = new Domain.Entities.Address
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

        var addressList = new List<Domain.Entities.Address> { address1, address2 };

        dbContext.AddRange(addressList);
        await dbContext.SaveChangesAsync();

        return addressList;
    }
}