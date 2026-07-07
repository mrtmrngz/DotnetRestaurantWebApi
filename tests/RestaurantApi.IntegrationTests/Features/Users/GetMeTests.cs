using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Users;


public class GetMeTests : BaseIntegrationTest
{
    public GetMeTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TEST START

    [Fact]
    public async Task GetUserInfoForLogin_WhenUserExist_ShouldReturnUser()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetMeQueryResult>>();

        result.Should().NotBeNull();
        result.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        result.Data.Name.Should().Be(setupResult.User.Name);
        result.Data.Email.Should().Be(setupResult.User.Email);

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cacheKey = CacheKeys.GetMeKey(setupResult.User.Id.ToString());
            var userInCache = await cacheService.GetAsync<GetMeQueryResult>(cacheKey);

            userInCache.Should().NotBeNull();
            userInCache.Email.Should().Be(result.Data.Email);
        }
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task GetUserInfoForLogin_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.GetAsync("api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetUserInfoForLogin_WhenUserNotExist_ShouldReturn404()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            dbContext.Users.Remove(setupResult.User);
            await dbContext.SaveChangesAsync();
        }

        var response = await Client.GetAsync("api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.ReadContentAsAsync<NotFoundException>();

        result.Should().NotBeNull();
        result.Code.Should().Be(Codes.NOT_FOUND);
        result.Message.Should().Be("Kullanıcı bulunamadı.");
    }
    
    // ERROR TESTS END
}