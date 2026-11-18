using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Users;

public class GetUserRolesTests : BaseIntegrationTest
{
    public GetUserRolesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetUserRoles_WhenUserExists_ShouldReturnAllRolesWithOwnershipFlags()
    {
        // Arrange: yalnızca USER rolüne bağlı bir kullanıcı
        var member = await CreateVanillaUserAsync();
        await LinkUserToRoleAsync(member.User.Id, AppRoles.User);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(member.User.Id));

        // Assert
        var body = await response.ReadContentAsAsync<
            GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        // istek tüm rolleri döner ve roller ada göre alfabetiktir
        var roleNames = body.Data.Select(role => role.Name).ToList();
        roleNames.Should().Contain(AppRoles.Admin).And.Contain(AppRoles.User);
        roleNames.Should().BeInAscendingOrder(StringComparer.Ordinal,
            "roller ada göre alfabetik dönmelidir.");

        // kullanıcının sahip olduğu rol true, olmadığı rol false işaretlenir
        body.Data.Single(role => role.Name == AppRoles.User).HasRole.Should().BeTrue(
            "kullanıcı USER rolüne bağlıdır.");
        body.Data.Single(role => role.Name == AppRoles.Admin).HasRole.Should().BeFalse(
            "kullanıcı ADMIN rolüne bağlı değildir.");

        // her satırda rol id ve rol adı doludur
        body.Data.Should().OnlyContain(role => role.Id != Guid.Empty && !string.IsNullOrWhiteSpace(role.Name));
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetUserRoles_WhenTokenIsMissing_ShouldReturn401()
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
    public async Task GetUserRoles_WhenUserHasNoUpdateUserPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> user:update izni yok
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(vanilla.User.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının user:update izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task GetUserRoles_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir kullanıcı id'si
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Kullanıcı bulunamadı.");
    }

    #endregion

    #region SETUP

    private static string Route(Guid userId) => $"/api/users/{userId}/roles";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // Kullanıcı, seed edilmiş rolün id'si ile AspNetUserRoles tablosunda eşleştirilir.
    private async Task LinkUserToRoleAsync(Guid userId, string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var roleId = await context.Roles
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .FirstAsync();

        context.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = roleId
        });

        await context.SaveChangesAsync();
    }

    #endregion
}
