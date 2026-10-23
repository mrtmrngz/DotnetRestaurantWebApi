
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class GetUserCartTotalTests : BaseIntegrationTest
{
    public const string Route = "/api/cart/users/total";

    public GetUserCartTotalTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetUserCartTotal_WhenUserIsAuthenticated_ShouldReturn200()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "İstek başarıyla tamamlandığı için 200 OK dönmeliydi.");

        var responseBody =
            await response.ReadContentAsAsync<
                GeneralSuccessResponseWithData<GetCartTotalQueryResult>>();

        responseBody.Should().NotBeNull(
            "API başarılı bir yanıt döndürdüğünde response body null olmamalıydı.");
        responseBody.Data.Should().NotBeNull();

        responseBody.Data.Should().NotBeNull(
            "Başarılı yanıtta sepet toplamı bilgileri bulunmalıydı.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetUserCartTotal_WhenUserNotLoggedIn_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 Unauthorized dönmeliydi.");
    }

    [Fact]
    public async Task GetUserCartTotal_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var user = await ctx.Users
                .FirstAsync(x => x.Id == userSetup.User.Id);

            ctx.Users.Remove(user);

            await ctx.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kullanıcı veritabanında bulunamadığı için 404 Not Found dönmeliydi.");
    }

    // ERROR TESTS END
}
