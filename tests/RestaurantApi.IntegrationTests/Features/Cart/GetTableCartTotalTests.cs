
using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Cart;

public class GetTableCartTotalTests : BaseIntegrationTest
{
    private const string Route = "/api/cart/tables/";

    public GetTableCartTotalTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetTableCartTotal_WhenValidRequest_ShouldReturn200()
    {
        // Arrange
        var userSetup = await CreateAdminUserAsync();
        var tableId = await SeedTableAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}{tableId}/total");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Yetkili kullanıcı mevcut masanın sepet toplamını sorguladığı için 200 OK dönmeliydi.");

        var responseBody =
            await response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetCartTotalQueryResult>>();

        responseBody.Should().NotBeNull(
            "Başarılı API isteğinde response body null gelmemeliydi.");

        responseBody.Data.Should().NotBeNull(
            "Başarılı API yanıtında sepet toplam bilgileri bulunmalıydı.");

        responseBody.Data.Total.Should().Be(0m,
            "Masanın henüz sepeti bulunmadığı için toplam tutar sıfır olmalıydı.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetTableCartTotal_WhenUserNotLoggedIn_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"{Route}{Guid.NewGuid()}/total");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 Unauthorized dönmeliydi.");
    }

    [Fact]
    public async Task GetTableCartTotal_WhenUserHasNotPermission_ShouldReturn403()
    {
        // Arrange
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}{Guid.NewGuid()}/total");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı masa sepet toplamını görüntülemek için gerekli izne sahip olmadığından 403 Forbidden dönmeliydi.");
    }

    [Fact]
    public async Task GetTableCartTotal_WhenTableDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var userSetup = await CreateAdminUserAsync();
        var nonExistingTableId = Guid.NewGuid();

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        // Act
        var response = await Client.GetAsync($"{Route}{nonExistingTableId}/total");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Belirtilen masa veritabanında bulunmadığı için 404 Not Found dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Guid> SeedTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();

        var ctx = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 2,
            IsActive = true,
            Number = 6
        };

        ctx.RestaurantTables.Add(table);

        await ctx.SaveChangesAsync();

        return table.Id;
    }
}
