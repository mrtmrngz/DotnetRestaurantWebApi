using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

public class UpdateRolePermissionsTests : BaseIntegrationTest
{
    public UpdateRolePermissionsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task UpdateRolePermissions_WhenPermissionsAreOnlyAdded_ShouldAssignRequestedPermissions()
    {
        // Arrange: izni olmayan özel rol + veritabanındaki iki izin; rolün izin cache'i önceden doldurulur
        var role = await SeedRoleAsync();
        var requestedPermissionIds = await PickPermissionIdsAsync(2);

        await SetPermissionCacheAsync(role.Name!);

        var cachedBefore = await GetPermissionCacheAsync(role.Name!);
        cachedBefore.Should().NotBeNull("test için cache anahtarı önceden doldurulmalıydı.");

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(role.Id), new { permissionIds = requestedPermissionIds });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var assignedPermissionIds = await GetAssignedPermissionIdsAsync(role.Id);
        var cachedPermissions = await GetPermissionCacheAsync(role.Name!);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        body.Message.Should().Be("Rol izinleri başarılı bir şekilde güncellendi.");

        assignedPermissionIds.Should().BeEquivalentTo(requestedPermissionIds,
            "istekteki izinlerin tamamı role eklenmelidir.");

        cachedPermissions.Should().BeNull(
            "rol izinleri güncellendiği için yetki kararını besleyen cache anahtarı silinmelidir.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenPermissionsAreOnlyRemoved_ShouldKeepOnlyRequestedOnes()
    {
        // Arrange: üç izni olan rol; istekte yalnızca ikisi gönderilir
        var initialPermissionIds = await PickPermissionIdsAsync(3);
        var role = await SeedRoleAsync(initialPermissionIds);

        var requestedPermissionIds = initialPermissionIds.Take(2).ToList();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(role.Id), new { permissionIds = requestedPermissionIds });

        // Assert
        var assignedPermissionIds = await GetAssignedPermissionIdsAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        assignedPermissionIds.Should().BeEquivalentTo(requestedPermissionIds,
            "istekte olmayan izin kaldırılmalı, istekteki izinler korunmalıdır.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenPermissionsAreAddedAndRemoved_ShouldApplyBothChanges()
    {
        // Arrange: rol A ve B izinlerine sahip; istekte B ve C gönderilir -> A silinir, C eklenir
        var allPermissionIds = await PickPermissionIdsAsync(3);
        var permissionA = allPermissionIds[0];
        var permissionB = allPermissionIds[1];
        var permissionC = allPermissionIds[2];

        var role = await SeedRoleAsync(new List<Guid> { permissionA, permissionB });

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(role.Id),
            new { permissionIds = new List<Guid> { permissionB, permissionC } });

        // Assert
        var assignedPermissionIds = await GetAssignedPermissionIdsAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        assignedPermissionIds.Should().BeEquivalentTo(new List<Guid> { permissionB, permissionC },
            "yalnızca istekteki izinler kalmalıdır: A silinir, C eklenir.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task UpdateRolePermissions_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PutAsJsonAsync(Route(Guid.NewGuid()),
            new { permissionIds = await PickPermissionIdsAsync(1) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenUserHasNoUpdateRolePermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:update izni yok
        var role = await SeedRoleAsync();
        var requestedPermissionIds = await PickPermissionIdsAsync(2);

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(role.Id), new { permissionIds = requestedPermissionIds });

        // Assert
        var assignedPermissionIds = await GetAssignedPermissionIdsAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:update izni bulunmadığı için 403 dönmelidir.");
        assignedPermissionIds.Should().BeEmpty("yetkisiz istek hiçbir izin yazmamalıdır.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenRoleDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir rol id'si
        var response = await Client.PutAsJsonAsync(Route(Guid.NewGuid()),
            new { permissionIds = await PickPermissionIdsAsync(2) });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Rol bulunamadı.");
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenRoleIsSystemRole_ShouldReturn422()
    {
        // Arrange: sistem rolü (ADMIN) seed ile yönetilir; izinleri değiştirilemez
        var adminRoleId = await GetSystemRoleIdAsync(AppRoles.Admin);
        var assignedPermissionIdsBefore = await GetAssignedPermissionIdsAsync(adminRoleId);

        await SetPermissionCacheAsync(AppRoles.Admin);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(adminRoleId),
            new { permissionIds = await PickPermissionIdsAsync(1) });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var assignedPermissionIdsAfter = await GetAssignedPermissionIdsAsync(adminRoleId);
        var cachedPermissions = await GetPermissionCacheAsync(AppRoles.Admin);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "sistem rolü bu uçtan güncellenemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Sistem rollerinin izinleri güncellenemez.");

        assignedPermissionIdsAfter.Should().BeEquivalentTo(assignedPermissionIdsBefore,
            "reddedilen istek sistem rolünün izinlerini değiştirmemelidir.");
        assignedPermissionIdsAfter.Should().NotBeEmpty("ADMIN rolü seed sırasında tüm izinlere bağlanır.");

        cachedPermissions.Should().BeEquivalentTo(CachedPermissionList,
            "reddedilen istekte hiçbir şey değişmediği için rol izin cache'i olduğu gibi korunmalıdır.");
    }

    [Fact]
    public async Task UpdateRolePermissions_WhenSomePermissionsDoNotExist_ShouldReturn404AndWriteNothing()
    {
        // Arrange: rolün bir izni var; istekte bir geçerli bir de geçersiz izin gönderilir
        var existingPermissionIds = await PickPermissionIdsAsync(1);
        var role = await SeedRoleAsync(existingPermissionIds);

        var missingPermissionId = Guid.NewGuid();
        var requestedPermissionIds = new List<Guid> { missingPermissionId, existingPermissionIds[0] };

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PutAsJsonAsync(Route(role.Id), new { permissionIds = requestedPermissionIds });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var assignedPermissionIds = await GetAssignedPermissionIdsAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "istekteki izinlerden biri bulunamadığı için 404 dönmelidir.");
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("İstekteki bazı izinler bulunamadı.");

        assignedPermissionIds.Should().BeEquivalentTo(existingPermissionIds,
            "geçersiz izin içeren istekte hiçbir değişiklik yazılmamalıdır.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region SETUP
    
    private static readonly List<string> CachedPermissionList = new() { "orders:read", "role:update" };

    private static string Route(Guid roleId) => $"/api/roles/{roleId}/permissions";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<AppRole> SeedRoleAsync(IReadOnlyCollection<Guid>? permissionIds = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var roleName = $"TEST_ROLE_{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsSystemRole = false
        };

        dbContext.Roles.Add(role);

        foreach (var permissionId in permissionIds ?? Array.Empty<Guid>())
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
            });
        }

        await dbContext.SaveChangesAsync();

        return role;
    }

    private async Task<Guid> GetSystemRoleIdAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await dbContext.Roles
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .FirstAsync();
    }

    private async Task<List<Guid>> PickPermissionIdsAsync(int count)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => permission.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task<List<Guid>> GetAssignedPermissionIdsAsync(Guid roleId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await dbContext.RolePermissions
            .AsNoTracking()
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync();
    }

    private async Task SetPermissionCacheAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.RolePermissions(roleName.ToLowerInvariant()), CachedPermissionList,
            TimeSpan.FromHours(1));
    }

    private async Task<List<string>?> GetPermissionCacheAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        return await cache.GetAsync<List<string>>(CacheKeys.RolePermissions(roleName.ToLowerInvariant()));
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; rol-izin bağlantıları cascade ile birlikte silinir.
    private async Task DeleteRoleAsync(Guid roleId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var role = await dbContext.Roles.FirstOrDefaultAsync(existing => existing.Id == roleId);

        if (role is null)
        {
            return;
        }

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync();
    }

    #endregion
}
