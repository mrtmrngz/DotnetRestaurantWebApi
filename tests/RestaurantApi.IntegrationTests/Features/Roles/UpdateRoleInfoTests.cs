using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

public class UpdateRoleInfoTests : BaseIntegrationTest
{
    public UpdateRoleInfoTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task UpdateRoleInfo_WhenNewNameIsAvailable_ShouldUpdateRoleAndClearCaches()
    {
        var member = await CreateVanillaUserAsync();
        var role = await SeedRoleAsync(new[] { member.User.Id });

        await SetPermissionCacheAsync(role.Name!);
        await SetUserRolesCacheAsync(member.User.Id);

        var newRoleName = UniqueRoleName();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PatchAsJsonAsync(Route(role.Id), new { name = newRoleName });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var updatedRole = await GetRoleAsync(role.Id);
        var oldPermissionCache = await GetPermissionCacheAsync(role.Name!);
        var userRolesCache = await GetUserRolesCacheAsync(member.User.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        body.Message.Should().Be($"'{newRoleName}' rolünün bilgileri başarılı bir şekilde güncellendi.");

        updatedRole.Name.Should().Be(newRoleName, "rol adı istekteki adla güncellenmelidir.");
        updatedRole.NormalizedName.Should().Be(newRoleName.ToUpperInvariant(),
            "normalize ad, rol adının büyük harfli hali olmalıdır.");

        oldPermissionCache.Should().BeNull("rol adı değiştiği için eski adın izin cache'i silinmelidir.");
        userRolesCache.Should().BeNull("rol adı değiştiği için kullanıcıların rol cache'i silinmelidir.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateRoleInfo_WhenRequestedNameEqualsCurrentName_ShouldReturnSuccessWithoutUpdating()
    {
        // Arrange: istekte rolün mevcut adı gönderilir
        var role = await SeedRoleAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PatchAsJsonAsync(Route(role.Id), new { name = role.Name });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var unchangedRole = await GetRoleAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "ad değişmediği için uç idempotent davranmalıdır.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        unchangedRole.Name.Should().Be(role.Name, "ad aynı olduğu için yazma yapılmamalıdır.");
        unchangedRole.NormalizedName.Should().Be(role.NormalizedName);

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task UpdateRoleInfo_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PatchAsJsonAsync(Route(Guid.NewGuid()), new { name = UniqueRoleName() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task UpdateRoleInfo_WhenUserHasNoUpdateRolePermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:update izni yok
        var role = await SeedRoleAsync();
        var newRoleName = UniqueRoleName();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.PatchAsJsonAsync(Route(role.Id), new { name = newRoleName });

        // Assert
        var unchangedRole = await GetRoleAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:update izni bulunmadığı için 403 dönmelidir.");
        unchangedRole.Name.Should().Be(role.Name, "yetkisiz istek rol adını değiştirmemelidir.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task UpdateRoleInfo_WhenRoleDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir rol id'si
        var response = await Client.PatchAsJsonAsync(Route(Guid.NewGuid()), new { name = UniqueRoleName() });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Rol bulunamadı.");
    }

    [Fact]
    public async Task UpdateRoleInfo_WhenRoleIsSystemRole_ShouldReturn422()
    {
        // Arrange: sistem rolü (ADMIN) seed ile yönetilir; adı değiştirilemez
        var adminRole = await GetRoleByNameAsync(AppRoles.Admin);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PatchAsJsonAsync(Route(adminRole.Id), new { name = UniqueRoleName() });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var unchangedRole = await GetRoleAsync(adminRole.Id);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "sistem rolü bu uçtan güncellenemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Sistem rollerinin bilgileri güncellenemez.");
        unchangedRole.Name.Should().Be(AppRoles.Admin, "reddedilen istek sistem rolünü değiştirmemelidir.");
    }

    [Fact]
    public async Task UpdateRoleInfo_WhenNameBelongsToAnotherRole_ShouldReturn409()
    {
        // Arrange: başka bir rolün adı istenir; karşılaştırma normalize ad üzerinden de yapılır
        var role = await SeedRoleAsync();
        var otherRole = await SeedRoleAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: diğer rolün adı küçük harfle gönderilir (normalize hali aynı)
        var response = await Client.PatchAsJsonAsync(Route(role.Id), new { name = otherRole.Name!.ToLowerInvariant() });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var unchangedRole = await GetRoleAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "rol adı başka bir rol tarafından kullanıldığı için 409 dönmelidir.");
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Bu rol adı zaten kullanılıyor.");
        unchangedRole.Name.Should().Be(role.Name, "çakışan istek rol adını değiştirmemelidir.");

        await DeleteRoleAsync(role.Id);
        await DeleteRoleAsync(otherRole.Id);
    }

    #endregion

    #region SETUP

    private static readonly List<string> CachedPermissionList = new() { "orders:read", "role:update" };

    private static string Route(Guid roleId) => $"/api/roles/{roleId}";

    private static string UniqueRoleName() => $"TEST_ROLE_{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; rol-izin ve kullanıcı-rol bağlantıları cascade ile birlikte silinir.
    private async Task<AppRole> SeedRoleAsync(IReadOnlyCollection<Guid>? userIds = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var roleName = UniqueRoleName();

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsSystemRole = false
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

        await context.SaveChangesAsync();

        return role;
    }

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

    private async Task<AppRole> GetRoleAsync(Guid roleId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().FirstAsync(role => role.Id == roleId);
    }

    private async Task<AppRole> GetRoleByNameAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().FirstAsync(role => role.Name == roleName);
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

    private async Task SetUserRolesCacheAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.UserRoles(userId.ToString()), new List<string> { "USER" },
            TimeSpan.FromHours(1));
    }

    private async Task<List<string>?> GetUserRolesCacheAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        return await cache.GetAsync<List<string>>(CacheKeys.UserRoles(userId.ToString()));
    }

    #endregion
}
