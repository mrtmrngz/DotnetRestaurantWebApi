using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;

namespace RestaurantApi.IntegrationTests.Features.Users;

public class GetUserListTests : BaseIntegrationTest
{
    private const string Route = "/api/users";

    public GetUserListTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetUserList_WhenUsersExist_ShouldReturnUsersWithRolesOrderedByName()
    {
        var admin = await CreateAdminUserAsync();
        var member = await CreateVanillaUserAsync(user =>
        {
            user.PhoneNumber = "+905551112233";
            user.TwoFactorEnabled = true;
        });

        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await response.ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Code.Should().Be(Codes.FETH_DATA_SUCCESS);
        body.Data.Should().HaveCount(2, "testte iki kullanıcı oluşturuldu.");

        body.Data[0].Id.Should().Be(admin.User.Id, "liste ad ve soyada göre alfabetik dönmelidir.");
        body.Data[1].Id.Should().Be(member.User.Id);

        var adminRow = body.Data[0];
        adminRow.FullName.Should().Be("Admin User");
        adminRow.Email.Should().Be(admin.User.Email);
        adminRow.EmailConfirmed.Should().BeTrue();
        adminRow.TwoFactorStatus.Should().BeFalse();
        adminRow.Roles.Should().BeEquivalentTo(new List<string> { AppRoles.Admin });

        var memberRow = body.Data[1];
        memberRow.FullName.Should().Be("John Doe");
        memberRow.Email.Should().Be(member.User.Email);
        memberRow.PhoneNumber.Should().Be("+905551112233");
        memberRow.EmailConfirmed.Should().BeTrue();
        memberRow.TwoFactorStatus.Should().BeTrue("TwoFactorStatus, kullanıcının iki adımlı doğrulama ayarıdır.");
        memberRow.Roles.Should().BeEmpty("kullanıcının hiçbir rolü yoktur.");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetUserList_WhenTokenIsMissing_ShouldReturn401()
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
    public async Task GetUserList_WhenUserHasNoViewUserPermission_ShouldReturn403()
    {
        // Arrange: normal kullanıcı (USER rolü) -> user:view izni yok
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının user:view izni bulunmadığı için 403 dönmelidir.");
    }

    #endregion

    #region SETUP

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    #endregion
}
