using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class RefreshTokenTests : BaseIntegrationTest
{
    public RefreshTokenTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TEST START

    [Fact]
    public async Task RefreshToken_WhenUserLoggedIn_ShouldRefreshToken()
    {
        var email = "test@mail.com";
        var password = "Test123";
        await SeedUserAsync(email, password);

        // login
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login response'ından Set-Cookie header'ını al
        var setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        setCookieHeader.Should().NotBeNull();

        // Cookie'yi parse et (_session=value kısmını al)
        var cookieValue = setCookieHeader.Split(';')[0];

        var pureRefreshToken = cookieValue.Split('=')[1];

        // Sonraki tüm request'lere otomatik eklenecek şekilde DefaultRequestHeaders'e ekle
        Client.DefaultRequestHeaders.Add("Cookie", cookieValue);

        var refreshResponse = await Client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await refreshResponse.Content.ReadFromJsonAsync<LoginControllerResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNull();
        result.Code.Should().Be(Codes.TOKEN_REFRESHED);

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cacheKey = CacheKeys.RefreshToken(pureRefreshToken);
            var oldToken = await cacheService.GetAsync<string>(cacheKey);

            oldToken.Should().BeNull();
        }
    }

    // SUCCESS TEST END    

    // ERROR TEST START

    [Fact]
    public async Task RefreshToken_WhenRefreshTokenNotExistInCookie_ShouldReturn401()
    {
        var refreshResponse = await Client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ERROR TEST END

    private async Task<AppUser> SeedUserAsync(string email, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        var user = new AppUser
        {
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            Name = "John",
            Surname = "Doe",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}