using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Roles;

public class CreateRoleTests : BaseIntegrationTest
{
    private const string Route = "/api/roles";

    public CreateRoleTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task CreateRole_WhenNameIsAvailable_ShouldCreateApplicationRole()
    {
        // Arrange
        var roleName = UniqueRoleName();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { name = roleName });

        // Assert
        var body = await response.ReadContentAsAsync<BaseResponse>();
        var createdRole = await GetRoleByNameAsync(roleName);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body!.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS);
        body.Message.Should().Be($"'{roleName}' rolü başarılı bir şekilde oluşturuldu.");

        createdRole.Name.Should().Be(roleName);
        createdRole.NormalizedName.Should().Be(roleName.ToUpperInvariant(),
            "normalize ad, rol adının büyük harfli hali olmalıdır.");
        createdRole.IsSystemRole.Should().BeFalse("uçtan yalnızca uygulama rolleri oluşturulabilir.");

        await DeleteRoleAsync(createdRole.Id);
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task CreateRole_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { name = UniqueRoleName() });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task CreateRole_WhenUserHasNoCreateRolePermission_ShouldReturn403AndCreateNothing()
    {
        var roleName = UniqueRoleName();

        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { name = roleName });

        // Assert
        var roleExists = await RoleExistsAsync(roleName);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının role:create izni bulunmadığı için 403 dönmelidir.");
        roleExists.Should().BeFalse("yetkisiz istek rol oluşturmamalıdır.");
    }

    [Fact]
    public async Task CreateRole_WhenNameIsEmpty_ShouldReturn400()
    {
        // Arrange
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act: boş ad doğrulamaya takılır
        var response = await Client.PostAsJsonAsync(Route, new { name = string.Empty });

        // Assert
        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body!.Errors.Select(error => error.Message)
            .Should().Contain("Rol adı zorunludur.");
    }

    [Fact]
    public async Task CreateRole_WhenANameIsAlreadyTaken_ShouldReturn409()
    {
        // Arrange: aynı ada sahip bir rol önceden oluşturulur
        var role = await SeedRoleAsync();

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.PostAsJsonAsync(Route, new { name = role.Name });

        // Assert
        var body = await response.ReadContentAsAsync<ErrorResponse>();
        var roleCountWithTheName = await CountRolesWithNameAsync(role.Name!);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "rol adı zaten kullanıldığı için 409 dönmelidir.");
        body!.Code.Should().Be(Codes.CONFLICT);
        body.Message.Should().Be("Bu rol adı zaten kullanılıyor.");
        roleCountWithTheName.Should().Be(1, "reddedilen istek ikinci bir rol yaratmamalıdır.");

        await DeleteRoleAsync(role.Id);
    }

    #endregion

    #region SETUP

    private static string UniqueRoleName() => $"TEST_ROLE_{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // AspNetRoles tablosu Respawn tarafından temizlenmediği için (TablesToIgnore) oluşturulan rol
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
        await context.SaveChangesAsync();

        return role;
    }

    private async Task<AppRole> GetRoleByNameAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().FirstAsync(role => role.Name == roleName);
    }

    private async Task<bool> RoleExistsAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().AnyAsync(role => role.Name == roleName);
    }

    private async Task<int> CountRolesWithNameAsync(string roleName)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        return await context.Roles.AsNoTracking().CountAsync(role => role.Name == roleName);
    }

    #endregion
}
