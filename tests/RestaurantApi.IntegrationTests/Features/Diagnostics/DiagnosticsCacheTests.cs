using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using RestaurantApi.Infrastructure.Cache;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;

namespace RestaurantApi.IntegrationTests.Features.Diagnostics;

public class DiagnosticsCacheTests : BaseIntegrationTest
{
    private const string Route = "/api/diagnostics/cache";

    public DiagnosticsCacheTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TEST

    [Fact]
    public async Task GetCacheStats_WhenUserHasPermission_ShouldReturnCacheCounters()
    {
        // Arrange: ADMIN rolü seed sırasında tüm izinlere bağlanır
        var admin = await CreateAdminUserAsync();
        SetBearerToken(admin.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        var body = await response.ReadContentAsAsync<CacheMetricsStats>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Hit.Should().BeGreaterThanOrEqualTo(0);
        body.Miss.Should().BeGreaterThanOrEqualTo(0);
        body.HitRatio.Should().EndWith("%", "oran yüzde metni olarak döner (ör. 85.52%).");
    }

    #endregion

    #region ERROR TESTS

    [Fact]
    public async Task GetCacheStats_WhenTokenIsMissing_ShouldReturn401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "uç anonim erişime kapalıdır ve JWT token gönderilmediği için 401 dönmelidir.");
    }

    [Fact]
    public async Task GetCacheStats_WhenUserHasNoViewDiagnosticsPermission_ShouldReturn403()
    {
        var vanilla = await CreateVanillaUserAsync();
        SetBearerToken(vanilla.AccessToken);

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "kullanıcının diagnostics:view izni bulunmadığı için 403 dönmelidir.");
    }

    #endregion

    #region SETUP

    private void SetBearerToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    #endregion
}
