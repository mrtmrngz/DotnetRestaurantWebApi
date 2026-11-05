using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Roles.Queries.GetRolePermissionsQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

// GET api/roles/{roleId}/permissions ucunun uçtan uca davranışı.
// Handler doğrudan çağrılmaz; istekler gerçek HTTP ile gönderilir ve sonuçlar veritabanıyla karşılaştırılır.
// Kapsam: role bağlı izinlerin listelenmesi (200), yetkilendirme (401/403) ve rol bulma (404).
public class GetRolePermissionsTests : BaseIntegrationTest
{
    public GetRolePermissionsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetRolePermissions_WhenRoleHasPermissions_ShouldReturnOnlyAssignedPermissionsOrderedByName()
    {
        // Arrange: üç izinden ikisi role bağlanır; üçüncüsü listede görünmemelidir
        var permissions = await PickPermissionsAsync(3);
        var assignedPermissions = permissions.Take(2).ToList();
        var unassignedPermission = permissions[2];

        var role = await SeedRoleAsync(assignedPermissions.Select(permission => permission.Id).ToList());

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(role.Id));

        // Assert
        var body = await response.ReadContentAsAsync<
            GeneralSuccessResponseWithData<IReadOnlyList<GetRolePermissionsQueryResult>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        var expectedPermissions = assignedPermissions
            .OrderBy(permission => permission.Name, StringComparer.Ordinal)
            .Select(permission => new GetRolePermissionsQueryResult
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description
            })
            .ToList();

        body.Data.Should().BeEquivalentTo(expectedPermissions, options => options.WithStrictOrdering())
            .And.HaveCount(2, "role yalnızca iki izne bağlıdır.");

        body.Data.Should().NotContain(permission => permission.Id == unassignedPermission.Id,
            "role bağlı olmayan izin listede dönmemelidir.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetRolePermissions_WhenTokenIsMissing_ShouldReturn401()
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
    public async Task GetRolePermissions_WhenUserHasNoViewRolePermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:view izni yok
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:view izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task GetRolePermissions_WhenRoleDoesNotExist_ShouldReturn404()
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

    private static string Route(Guid roleId) => $"/api/roles/{roleId}/permissions";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; rol-izin bağlantıları cascade ile birlikte silinir.
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

    private async Task<AppRole> SeedRoleAsync(IReadOnlyCollection<Guid>? permissionIds = null)
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

        foreach (var permissionId in permissionIds ?? Array.Empty<Guid>())
        {
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await context.SaveChangesAsync();

        return role;
    }

    private async Task<List<Permission>> PickPermissionsAsync(int count)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Take(count)
            .ToListAsync();
    }

    #endregion
}
