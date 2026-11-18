using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Users;

public class GetUserInfoTests : BaseIntegrationTest
{
    public GetUserInfoTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetUserInfo_WhenUserExists_ShouldReturnUserInfoWithRoles()
    {
        // Arrange: iletişim/doğrulama alanları dolu ve bir role bağlı kullanıcı
        var member = await CreateVanillaUserAsync(user =>
        {
            user.PhoneNumber = "+905551112233";
            user.PhoneNumberConfirmed = true;
            user.TwoFactorEnabled = true;
        });

        await LinkUserToRoleAsync(member.User.Id, AppRoles.User);

        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(member.User.Id));

        // Assert
        var body = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<GetUserInfoQueryResult>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);

        var data = body.Data;
        data.Id.Should().Be(member.User.Id);
        data.Name.Should().Be("John");
        data.Surname.Should().Be("Doe");
        data.Email.Should().Be(member.User.Email);
        data.EmailConfirmed.Should().BeTrue();
        data.PhoneNumber.Should().Be("+905551112233");
        data.PhoneNumberConfirmed.Should().BeTrue();
        data.TwoFactorEnabled.Should().BeTrue();
        data.Roles.Should().BeEquivalentTo(new List<string> { AppRoles.User });
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetUserInfo_WhenTokenIsMissing_ShouldReturn401()
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
    public async Task GetUserInfo_WhenUserHasNoViewUserPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> user:view izni yok
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route(vanilla.User.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının user:view izni bulunmadığı için 403 dönmelidir.");
    }

    [Fact]
    public async Task GetUserInfo_WhenUserDoesNotExist_ShouldReturn404()
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

    private static string Route(Guid userId) => $"/api/users/{userId}";

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
