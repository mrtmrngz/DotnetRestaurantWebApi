using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

public class GetRoleListTests : BaseIntegrationTest
{
    private const string Route = "/api/roles";

    public GetRoleListTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task GetRoleList_WhenRequested_ShouldReturnSeededSystemRoles()
    {
        // Arrange: sistem rolleri (ADMIN/USER) seed edilmiş; ADMIN tüm izinlere sahip
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        var adminRole = body.Data.FirstOrDefault(role => role.Name == AppRoles.Admin);
        var userRole = body.Data.FirstOrDefault(role => role.Name == AppRoles.User);

        adminRole.Should().NotBeNull("ADMIN rolü seed edildiği için listede dönmelidir.");
        userRole.Should().NotBeNull("USER rolü seed edildiği için listede dönmelidir.");

        adminRole!.IsSystemRole.Should().BeTrue("ADMIN statik rollerden biridir.");
        userRole!.IsSystemRole.Should().BeTrue("USER statik rollerden biridir.");

        adminRole.PermissionCount.Should().BeGreaterThan(0,
            "ADMIN rolü seed sırasında tüm izinlere bağlanır.");
    }

    [Fact]
    public async Task GetRoleList_WhenRolesExist_ShouldReturnRolesOrderedByName()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data.Should().NotBeEmpty();

        var names = body.Data.Select(role => role.Name).ToList();

        names.Should().BeInAscendingOrder(StringComparer.Ordinal,
            "rol listesi Name alanına göre alfabetik dönmelidir.");
    }

    [Fact]
    public async Task GetRoleList_WhenRoleHasUsers_ShouldReturnUserCount()
    {
        // Arrange: iki kullanıcı özel bir role bağlanır; ADMIN rolünde ise test admin'i vardır
        var roleName = UniqueRoleName();

        var firstUser = await CreateVanillaUserAsync();
        var secondUser = await CreateVanillaUserAsync();

        await SeedRoleAsync(roleName, new[] { firstUser.User.Id, secondUser.User.Id });

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customRole = body!.Data.SingleOrDefault(role => role.Name == roleName);

        customRole.Should().NotBeNull();
        customRole!.UserCount.Should().Be(2, "role bağlı iki kullanıcı vardır.");

        var adminRole = body.Data.Single(role => role.Name == AppRoles.Admin);
        adminRole.UserCount.Should().BeGreaterThan(0, "test admin'i ADMIN rolüne bağlıdır.");

        await DeleteRoleAsync(roleName);
    }

    [Fact]
    public async Task GetRoleList_WhenRoleIsNotSystemRole_ShouldReturnIsSystemRoleFalseWithPermissionCount()
    {
        // Arrange: uygulamada sonradan oluşturulmuş, iki izne bağlı bir rol
        var roleName = UniqueRoleName();
        await SeedRoleAsync(roleName, permissionCount: 2);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customRole = body!.Data.Single(role => role.Name == roleName);

        customRole.IsSystemRole.Should().BeFalse(
            "rol AppRoles içindeki statik rollerden biri değildir.");
        customRole.PermissionCount.Should().Be(2, "role iki izin bağlanmıştır.");

        await DeleteRoleAsync(roleName);
    }

    [Fact]
    public async Task GetRoleList_WhenRoleHasNoUsersAndPermissions_ShouldReturnZeros()
    {
        // Arrange: hiç kullanıcısı ve izni olmayan yeni rol
        var roleName = UniqueRoleName();
        await SeedRoleAsync(roleName);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var customRole = body!.Data.Single(role => role.Name == roleName);

        customRole.UserCount.Should().Be(0, "role bağlı kullanıcı bulunmamaktadır.");
        customRole.PermissionCount.Should().Be(0, "role bağlı izin bulunmamaktadır.");
        customRole.IsSystemRole.Should().BeFalse();

        await DeleteRoleAsync(roleName);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetRoleList_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetRoleList_WhenUserHasNoViewRolePermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:view izni yok
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:view izni bulunmadığı için 403 dönmelidir.");
    }

    #endregion

    #region SETUP

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static Task<GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>?> ReadBodyAsync(
        HttpResponseMessage response)
    {
        return response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>>();
    }

    private static string UniqueRoleName()
    {
        return $"TEST_ROLE_{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    }

    private async Task<AppRole> SeedRoleAsync(string name, IReadOnlyCollection<Guid>? userIds = null,
        int permissionCount = 0)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        context.Roles.Add(role);

        foreach (var userId in userIds ?? Array.Empty<Guid>())
        {
            context.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = role.Id
            });
        }

        if (permissionCount > 0)
        {
            var permissionIds = await context.Permissions
                .OrderBy(permission => permission.Name)
                .Select(permission => permission.Id)
                .Take(permissionCount)
                .ToListAsync();

            context.RolePermissions.AddRange(permissionIds.Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            }));
        }

        await context.SaveChangesAsync();

        return role;
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; aksi halde artık rol ve izin bağlantıları sonraki testlere sızar.
    private async Task DeleteRoleAsync(string name)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var role = await context.Roles.FirstOrDefaultAsync(existing => existing.Name == name);

        if (role is null)
        {
            return;
        }

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
    }

    #endregion
}
