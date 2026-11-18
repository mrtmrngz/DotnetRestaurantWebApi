using System.Net;
using System.Net.Http.Headers;
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

// DELETE api/roles/{roleId} ucunun uçtan uca davranışı.
// Handler doğrudan çağrılmaz; istekler gerçek HTTP ile gönderilir ve sonuçlar veritabanından doğrulanır.
// Kapsam: rol silme ve cache temizliği (200), doğrulama (400), yetkilendirme (401/403), rol bulma (404),
// sistem rolü koruması (422) ve kullanıcısı olan rolün silinememesi (409).
public class RemoveRoleTests : BaseIntegrationTest
{
    public RemoveRoleTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task RemoveRole_WhenRoleIsDeletable_ShouldDeleteRoleAndClearCache()
    {
        // Arrange: kullanıcısı olmayan uygulama rolü ve önceden doldurulmuş izin cache'i
        var role = await SeedRoleAsync();

        await SetPermissionCacheAsync(role.Name!);

        var cachedBefore = await GetPermissionCacheAsync(role.Name!);
        cachedBefore.Should().NotBeNull("test için cache anahtarı önceden doldurulmalıydı.");

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route(role.Id));

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var roleExists = await RoleExistsAsync(role.Id);
        var cachedPermissions = await GetPermissionCacheAsync(role.Name!);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS);
        body.Message.Should().Be($"'{role.Name}' rolü başarılı bir şekilde silindi.");

        roleExists.Should().BeFalse("rol veritabanından silinmelidir.");
        cachedPermissions.Should().BeNull("rol silindiği için izin cache'i temizlenmelidir.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task RemoveRole_WhenRoleIdIsEmpty_ShouldReturn400()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: route kısıtına uyan ama boş olan rol id'si doğrulamaya takılır
        var response = await Client.DeleteAsync(Route(Guid.Empty));

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body!.Errors.Select(error => error.Message)
            .Should().Contain("Rol ID bilgisi zorunludur ve boş olamaz.");
    }

    [Fact]
    public async Task RemoveRole_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.DeleteAsync(Route(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task RemoveRole_WhenUserHasNoDeleteRolePermission_ShouldReturn403AndKeepRole()
    {
        // Arrange: normal kullanıcı (USER rolü) -> role:delete izni yok
        var role = await SeedRoleAsync();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route(role.Id));

        // Assert
        var roleExists = await RoleExistsAsync(role.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:delete izni bulunmadığı için 403 dönmelidir.");
        roleExists.Should().BeTrue("yetkisiz istek rolü silmemelidir.");

        await DeleteRoleAsync(role.Id);
    }

    [Fact]
    public async Task RemoveRole_WhenRoleDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir rol id'si
        var response = await Client.DeleteAsync(Route(Guid.NewGuid()));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Rol bulunamadı.");
    }

    [Fact]
    public async Task RemoveRole_WhenRoleIsSystemRole_ShouldReturn422AndKeepRole()
    {
        // Arrange: sistem rolü (ADMIN) seed ile yönetilir; silinemez
        var adminRole = await GetRoleByNameAsync(AppRoles.Admin);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route(adminRole.Id));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var roleExists = await RoleExistsAsync(adminRole.Id);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "sistem rolü bu uçtan silinemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Sistem rolleri silinemez.");
        roleExists.Should().BeTrue("reddedilen istek sistem rolünü silmemelidir.");
    }

    [Fact]
    public async Task RemoveRole_WhenRoleHasUsers_ShouldReturn409AndKeepRoleAndCache()
    {
        // Arrange: role bağlı bir kullanıcı ve önceden doldurulmuş izin cache'i
        var member = await CreateVanillaUserAsync();
        var role = await SeedRoleAsync(new[] { member.User.Id });

        await SetPermissionCacheAsync(role.Name!);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.DeleteAsync(Route(role.Id));

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var roleExists = await RoleExistsAsync(role.Id);
        var cachedPermissions = await GetPermissionCacheAsync(role.Name!);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "role bağlı kullanıcılar olduğu için 409 dönmelidir.");
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Role bağlı kullanıcılar bulunduğu için rol silinemez.");

        roleExists.Should().BeTrue("reddedilen istek rolü silmemelidir.");
        cachedPermissions.Should().BeEquivalentTo(CachedPermissionList,
            "reddedilen istekte hiçbir şey değişmediği için rol izin cache'i olduğu gibi korunmalıdır.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region SETUP

    private static readonly List<string> CachedPermissionList = new() { "orders:read" };

    private static string Route(Guid roleId) => $"/api/roles/{roleId}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) test rolü
    // test sonunda silinir; rol-izin ve kullanıcı-rol bağlantıları cascade ile birlikte silinir.
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

    private async Task<AppRole> SeedRoleAsync(IReadOnlyCollection<Guid>? userIds = null)
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

    private async Task<AppRole> GetRoleByNameAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().FirstAsync(role => role.Name == roleName);
    }

    private async Task<bool> RoleExistsAsync(Guid roleId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().AnyAsync(role => role.Id == roleId);
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

    #endregion
}
