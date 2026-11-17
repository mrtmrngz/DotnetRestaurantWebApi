using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

public class GetRoleInfoTests : BaseIntegrationTest
{
    public GetRoleInfoTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetRoleInfo_WhenRoleExists_ShouldReturnRoleInfo()
    {
        // Arrange
        var role = await SeedRoleAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(role.Id));

        // Assert
        var body = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetRoleInfoQueryResult>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        body.Data.Id.Should().Be(role.Id);
        body.Data.Name.Should().Be(role.Name);
        body.Data.NormalizedName.Should().Be(role.NormalizedName);
        body.Data.IsSystemRole.Should().BeFalse("uçtan oluşturulan uygulama rolleri sistem rolü değildir.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetRoleInfo_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetRoleInfo_WhenUserHasNoViewRolePermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:view izni yok
        var role = await SeedRoleAsync();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(role.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:view izni bulunmadığı için 403 dönmelidir.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task GetRoleInfo_WhenRoleDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir rol id'si
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Rol bulunamadı.");
    }

    #endregion

    #region SETUP

    private static string Route(Guid roleId) => $"/api/roles/{roleId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; aksi halde artık roller sonraki testlere sızar.
    private async Task DeleteRoleAsync(Guid roleId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var role = await context.Roles.FirstOrDefaultAsync(existing => existing.Id == roleId);

        if (role is null)
        {
            return;
        }

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
    }

    private async Task<AppRole> SeedRoleAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var roleName = $"TEST_ROLE_{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsSystemRole = false
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        return role;
    }

    #endregion
}
