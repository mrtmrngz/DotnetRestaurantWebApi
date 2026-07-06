using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class LogoutSetupResponseDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}

public class LogoutTest: BaseIntegrationTest
{
    public LogoutTest(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // TESTS START

    [Fact]
    public async Task Logout_WhenTokenInCache_ShouldReturnOKAndRemoveTokenAndUserRolesFromCache()
    {
        var setupResult = await LoginSetupForLogout();

        Client.DefaultRequestHeaders.Add("Cookie", $"_session={setupResult.Token}");
        var response = await Client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "200 başarılı kodu dönmeli.");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var userIdInCache = await cacheService.GetAsync<string>(CacheKeys.RefreshToken(setupResult.Token));
            userIdInCache.Should().BeNull("Kullanıcının id'si redisten silinmiş olmalı.");

            var userRolesInCache = await cacheService.GetAsync<IList<string>>(CacheKeys.UserRoles(setupResult.UserId.ToString()));
            userRolesInCache.Should().BeNull("Kullanıcının rolleri redisten silinmiş olmalı.");

            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var tokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == setupResult.Token && x.UserId == setupResult.UserId);

            tokenInDb.Should().NotBeNull("Token dbde bulunmalı.");
            tokenInDb.IsRevoked.Should().BeTrue("Veritabındaki token geçersiz sayılmalı.");
        }
    }

    [Fact]
    public async Task Logout_WhenTokenNotExist_ShouldReturnOK()
    {
        var setupResult = await LoginSetupForLogout();
        
        var response = await Client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var tokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == setupResult.Token && x.UserId == setupResult.UserId);
            tokenInDb.Should().NotBeNull("Token dbde bulunmalı.");
            tokenInDb.IsRevoked.Should().BeFalse("Cookie de token olmadığı için veri tabanındaki token geçersiz sayılmamalı.");
        }
    }
    
    // TESTS END
    
    // SETUP
    private async Task<LogoutSetupResponseDto> LoginSetupForLogout()
    {
        var email = "test@mail.com";
        var user = new AppUser()
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

        var password = "Test123";

        using (var scope = Factory.Services.CreateScope())
        {
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            await userRepo.CreateAsync(user, password);
        }

        var loginCommand = new LoginCommand(user.Email, password);

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginCommand);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var setCookieHeader = response.Headers.GetValues("Set-Cookie");
        setCookieHeader.Should().NotBeNull("Giriş başarılı olduğunda Set-Cookie header'ı dönmeliydi.");

        var sessionCookie = setCookieHeader.FirstOrDefault(c => c.StartsWith("_session"));
        sessionCookie.Should().NotBeNull("Dönen cookie'lerin arasında '_session' adında bir cookie bulunmalı.");

        var rawCookieValue = sessionCookie!.Split(';')[0];
        var refreshTokenFromCookie = rawCookieValue.Split('=')[1];

        refreshTokenFromCookie.Should().NotBeNullOrEmpty("Cookie içerisindeki refresh token boş olamaz.");
        sessionCookie.Should().Contain("httponly", "Güvenlik gereği refresh token cookie'si HttpOnly olmalıdır!");

        return new LogoutSetupResponseDto(){ UserId = user.Id, Token = refreshTokenFromCookie };
    }
}