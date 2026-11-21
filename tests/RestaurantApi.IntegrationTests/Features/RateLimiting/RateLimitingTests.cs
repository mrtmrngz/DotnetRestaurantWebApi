using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;

namespace RestaurantApi.IntegrationTests.Features.RateLimiting;


public class RateLimitingTests : BaseIntegrationTest
{
    public RateLimitingTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    #region SUCCESS TESTS

    [Fact]
    public async Task AuthLogin_WhenPermitLimitExceeded_ShouldReturn429WithBodyAndRetryAfter()
    {
        // Arrange: IP bazlı Auth.General policy'sinin limiti 1'e indirilir.
        using var env = new EnvironmentOverride(new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:Auth:General:PermitLimit"] = "1",
            ["RateLimiting:Auth:General:WindowSeconds"] = "60"
        });
        await using var factory = Fixture.CreateFactory();
        using var client = factory.CreateClient();

        var command = new LoginCommand("rate_limit_auth@mail.com", "WrongPass123");

        // Act: Limit içindeki ilk istek işlenir, ikinci istek limiti aşar.
        var firstResponse = await client.PostAsJsonAsync("/api/auth/login", command);
        var secondResponse = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        firstResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "ilk istek permit limiti içinde olduğu için 429 ile reddedilmemeliydi.");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "permit limiti aşıldığında uç nokta 429 TOO MANY REQUESTS dönmeliydi.");

        var body = await secondResponse.ReadContentAsAsync<RateLimitRejectionResponse>();

        body.Should().NotBeNull();
        body!.Status.Should().Be(429);
        body.Code.Should().Be(Codes.TOO_MANY_REQUEST,
            "reddedilen isteğin gövdesinde TOO_MANY_REQUEST kodu dönmeliydi.");
        body.Message.Should().NotBeNullOrWhiteSpace();

        secondResponse.Headers.RetryAfter.Should().NotBeNull(
            "istemcinin ne zaman tekrar deneyeceğini bilmesi için Retry-After başlığı eklenmeliydi.");
        secondResponse.Headers.RetryAfter!.Delta.Should().NotBeNull();
        secondResponse.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SameUserPolicy_ShouldKeepSeparateBudgetPerUser()
    {
        // Arrange: kullanıcı bazlı SameUser.Restricted policy'sinin limiti 1'e indirilir.
        using var env = new EnvironmentOverride(new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:SameUser:Restricted:PermitLimit"] = "1",
            ["RateLimiting:SameUser:Restricted:WindowSeconds"] = "60"
        });
        await using var factory = Fixture.CreateFactory();
        using var client = factory.CreateClient();

        var firstUser = await CreateVanillaUserAsync();
        var secondUser = await CreateVanillaUserAsync();

        // Act - ilk kullanıcı kendi limitini doldurur.
        SetBearerToken(client, firstUser.AccessToken);
        var firstUserFirstResponse = await client.GetAsync("/api/profile");
        var firstUserSecondResponse = await client.GetAsync("/api/profile");

        // Act - ikinci kullanıcı aynı uç noktayı çağırır (kendi partition'ı).
        SetBearerToken(client, secondUser.AccessToken);
        var secondUserResponse = await client.GetAsync("/api/profile");

        // Assert
        firstUserFirstResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "kullanıcının limiti içindeki ilk istek reddedilmemeliydi.");
        firstUserSecondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "aynı kullanıcının permit limiti aşıldığında 429 dönmeliydi.");
        secondUserResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "kullanıcı bazlı partition sayesinde diğer kullanıcının limiti etkilenmemeliydi.");
    }

    [Fact]
    public async Task PublicFlexiblePolicy_ShouldPartitionPerEndpoint()
    {
        // Arrange: per-endpoint uygulanan Public.Flexible policy'sinin limiti 1'e indirilir.
        using var env = new EnvironmentOverride(new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:Public:Flexible:PermitLimit"] = "1",
            ["RateLimiting:Public:Flexible:WindowSeconds"] = "60"
        });
        await using var factory = Fixture.CreateFactory();
        using var client = factory.CreateClient();

        // Act: Aynı policy'yi kullanan iki farklı uç noktaya istek atılır.
        var productsListResponse = await client.GetAsync("/api/products/public");
        var productDetailResponse = await client.GetAsync("/api/products/public/olmayan-urun");
        var productsListAgainResponse = await client.GetAsync("/api/products/public");

        // Assert
        productsListResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "ilk uç noktanın ilk isteği reddedilmemeliydi.");
        productDetailResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
            "per-endpoint partition sayesinde farklı uç noktalar aynı limiti paylaşmamalıydı.");
        productsListAgainResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "aynı uç nokta tekrar çağrıldığında permit limiti aşıldığı için 429 dönmeliydi.");
    }

    #endregion

    #region DISABLED TESTS

    [Fact]
    public async Task WhenDisabled_ShouldNotRejectRequestsEvenIfLimitsExceeded()
    {
        // Arrange: limitler çok düşük olsa bile Enabled=false iken hiçbir istek reddedilmemeli.
        using var env = new EnvironmentOverride(new Dictionary<string, string>
        {
            ["RateLimiting:Enabled"] = "false",
            ["RateLimiting:Auth:General:PermitLimit"] = "1",
            ["RateLimiting:Auth:General:WindowSeconds"] = "60"
        });
        await using var factory = Fixture.CreateFactory();
        using var client = factory.CreateClient();

        var command = new LoginCommand("rate_limit_disabled@mail.com", "WrongPass123");

        // Act & Assert
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", command);

            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                "rate limiting kapalıyken hiçbir istek 429 ile reddedilmemeliydi.");
        }
    }

    #endregion

    #region SETUP

    // rate limit test setup
    private sealed class EnvironmentOverride : IDisposable
    {
        private readonly Dictionary<string, string?> _previousValues = new();

        public EnvironmentOverride(IDictionary<string, string> settings)
        {
            foreach (var pair in settings)
            {
                var key = pair.Key.Replace(":", "__");
                _previousValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, pair.Value);
            }
        }

        public void Dispose()
        {
            foreach (var pair in _previousValues)
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }

    private static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    // rate limiting response
    private sealed class RateLimitRejectionResponse
    {
        public int Status { get; init; }
        public Codes Code { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    #endregion
}
