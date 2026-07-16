using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Profile;

public class ProfileInfoTests: BaseIntegrationTest
{
    public ProfileInfoTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task ProfileInfo_WhenUserExist_ShouldReturnUser()
    {
        var setupResult = await CreateVanillaUserAsync(u => u.PhoneNumber = "+901234567889");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);
        
        var response = await Client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<ProfileInfoQueryResult>>();

        result.Should().NotBeNull("Response null olmamalı.");
        result.Code.Should().Be(Codes.FETH_DATA_SUCCESS, "Response body içerisindeki code FETCH_DATA_SUCCESS olmalı.");
        result.Data.Email.Should().Be(setupResult.User.Email, "Setup resulttaki user ile dönen userin mail adresi eşleşmeli.");
        result.Data.Id.Should().Be(setupResult.User.Id, "Setup resulttaki user ile dönen userin id'si eşleşmeli.");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var cacheKey = CacheKeys.ProfileInfoKey(setupResult.User.Id.ToString());
            var userInCache = await cacheService.GetAsync<ProfileInfoQueryResult>(cacheKey);
            userInCache.Should().NotBeNull("User rediste olmalı.");
            userInCache.Id.Should().Be(setupResult.User.Id, "Redisteki user id ile setup resulttaki id eşleşmeli.");
        }
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task ProfileInfo_WhenUserNotAuthenticate_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProfileInfo_WhenUserNotExist_ShouldReturn404()
    {
        var setupResult = await CreateVanillaUserAsync(u => u.PhoneNumber = "+901234567889");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            context.Users.Remove(setupResult.User);
            await context.SaveChangesAsync();
        }
        
        var response = await Client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.ReadContentAsAsync<NotFoundException>();

        result.Should().NotBeNull("Result boş olmamalı");
        result.Code.Should().Be(Codes.NOT_FOUND, "Response içerisinden dönen code NOT_FOUND olmalı.");
        
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var cacheKey = CacheKeys.ProfileInfoKey(setupResult.User.Id.ToString());
            var userInCache = await cacheService.GetAsync<ProfileInfoQueryResult>(cacheKey);
            userInCache.Should().BeNull("User rediste olmamalı.");
        }
    }
    
    // ERROR TESTS END
    
}