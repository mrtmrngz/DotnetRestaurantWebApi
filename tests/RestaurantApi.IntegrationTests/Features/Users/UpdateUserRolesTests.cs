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
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Users;

public class UpdateUserRolesTests : BaseIntegrationTest
{
    public UpdateUserRolesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task UpdateUserRoles_WhenRolesChange_ShouldReplaceRolesAndClearCache()
    {
        // Arrange: USER rolüne bağlı kullanıcı ve dolu rol cache'i
        var user = await CreateVanillaUserAsync();
        await LinkUserToRoleAsync(user.User.Id, AppRoles.User);

        await SetUserRolesCacheAsync(user.User.Id);

        var adminRoleId = await GetRoleIdAsync(AppRoles.Admin);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: istekte yalnızca ADMIN gönderilir -> USER kaldırılır, ADMIN eklenir
        var response = await PutAsync(user.User.Id, new[] { adminRoleId });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var roleIds = await GetUserRoleIdsAsync(user.User.Id);
        var cachedRoles = await GetUserRolesCacheAsync(user.User.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);
        body.Message.Should().Be("Kullanıcının rolleri başarılı bir şekilde güncellendi.");

        roleIds.Should().BeEquivalentTo(new List<Guid> { adminRoleId },
            "istekte olmayan roller kaldırılmalı, istekteki roller eklenmelidir.");
        cachedRoles.Should().BeNull(
            "roller değiştiği için kullanıcının rol cache'i (roles:{userId}) temizlenmelidir.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenRolesAreUnchanged_ShouldReturnSuccessWithoutWriting()
    {
        // Arrange: USER rolüne bağlı kullanıcı ve dolu rol cache'i
        var user = await CreateVanillaUserAsync();
        await LinkUserToRoleAsync(user.User.Id, AppRoles.User);

        await SetUserRolesCacheAsync(user.User.Id);

        var userRoleId = await GetRoleIdAsync(AppRoles.User);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: kullanıcının sahip olduğu rolün aynısı gönderilir
        var response = await PutAsync(user.User.Id, new[] { userRoleId });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var roleIds = await GetUserRoleIdsAsync(user.User.Id);
        var cachedRoles = await GetUserRolesCacheAsync(user.User.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "değişiklik olmadığı için uç idempotent davranmalıdır.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS);

        roleIds.Should().BeEquivalentTo(new List<Guid> { userRoleId }, "roller aynı olduğu için değişmemelidir.");
        cachedRoles.Should().BeEquivalentTo(CachedUserRoleList,
            "değişiklik olmadığı için cache gereksiz yere temizlenmemelidir.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task UpdateUserRoles_WhenRoleListIsEmpty_ShouldReturn400()
    {
        // Arrange
        var user = await CreateVanillaUserAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: boş liste doğrulamaya takılır (en az bir rol zorunludur)
        var response = await PutAsync(user.User.Id, Array.Empty<Guid>());

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body!.Errors.Select(error => error.Message).Should().Contain("Kullanıcıya en az 1 rol atanmalıdır.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await PutAsync(Guid.NewGuid(), new[] { await GetRoleIdAsync(AppRoles.User) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenUserHasNoUpdateUserPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> user:update izni yok
        var user = await CreateVanillaUserAsync();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await PutAsync(user.User.Id, new[] { await GetRoleIdAsync(AppRoles.User) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının user:update izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: olmayan bir kullanıcı id'si
        var response = await PutAsync(Guid.NewGuid(), new[] { await GetRoleIdAsync(AppRoles.User) });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("Kullanıcı bulunamadı.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenUserIsSystemUser_ShouldReturn422AndKeepRoles()
    {
        // Arrange: sistem kullanıcısı (root admin) hedeflenir
        var systemUser = await CreateAdminUserAsync(user => user.IsSystemUser = true);
        var roleIdsBefore = await GetUserRoleIdsAsync(systemUser.User.Id);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PutAsync(systemUser.User.Id, new[] { await GetRoleIdAsync(AppRoles.User) });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var roleIdsAfter = await GetUserRoleIdsAsync(systemUser.User.Id);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "sistem kullanıcısının rolleri bu uçtan güncellenemez.");
        body!.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY);
        body.Message.Should().Be("Sistem kullanıcısının rolleri güncellenemez.");

        roleIdsAfter.Should().BeEquivalentTo(roleIdsBefore, "reddedilen istek rolleri değiştirmemelidir.");
    }

    [Fact]
    public async Task UpdateUserRoles_WhenSomeRolesDoNotExist_ShouldReturn404AndKeepRolesAndCache()
    {
        // Arrange: biri geçerli biri olmayan rol id'si gönderilir
        var user = await CreateVanillaUserAsync();
        await LinkUserToRoleAsync(user.User.Id, AppRoles.User);

        await SetUserRolesCacheAsync(user.User.Id);

        var userRoleId = await GetRoleIdAsync(AppRoles.User);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await PutAsync(user.User.Id, new[] { userRoleId, Guid.NewGuid() });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var roleIds = await GetUserRoleIdsAsync(user.User.Id);
        var cachedRoles = await GetUserRolesCacheAsync(user.User.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "istekteki rollerden biri bulunamadığı için 404 dönmelidir.");
        body!.Code.Should().Be(Codes.NOT_FOUND);
        body.Message.Should().Be("İstekteki bazı roller bulunamadı.");

        roleIds.Should().BeEquivalentTo(new List<Guid> { userRoleId },
            "geçersiz rol içeren istekte hiçbir değişiklik yazılmamalıdır.");
        cachedRoles.Should().BeEquivalentTo(CachedUserRoleList,
            "reddedilen istekte hiçbir şey değişmediği için rol cache'i korunmalıdır.");
    }

    #endregion

    #region SETUP

    private static readonly List<string> CachedUserRoleList = new() { AppRoles.User };

    private static string Route(Guid userId) => $"/api/users/{userId}/roles";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<HttpResponseMessage> PutAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        var payload = new { roleIds = roleIds.Select(roleId => roleId.ToString()).ToList() };

        return await Client.PutAsJsonAsync(Route(userId), payload);
    }

    private async Task<Guid> GetRoleIdAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles
            .AsNoTracking()
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .FirstAsync();
    }

    private async Task LinkUserToRoleAsync(Guid userId, string roleName)
    {
        var roleId = await GetRoleIdAsync(roleName);

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = roleId
        });

        await context.SaveChangesAsync();
    }

    private async Task<List<Guid>> GetUserRoleIdsAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.RoleId)
            .ToListAsync();
    }

    private async Task SetUserRolesCacheAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.UserRoles(userId.ToString()), CachedUserRoleList, TimeSpan.FromHours(1));
    }

    private async Task<List<string>?> GetUserRolesCacheAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        return await cache.GetAsync<List<string>>(CacheKeys.UserRoles(userId.ToString()));
    }

    #endregion
}
