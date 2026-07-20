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
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Features.ProfileHandlers.Commands.ChangePasswordCommand;
using RestaurantApi.Application.Features.ProfileHandlers.Queries.ProfileInfoQuery;
using RestaurantApi.Application.Features.Users.Queries.GetMeQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Profile;

public class SetupResult
{
    public AppUser User { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

public class ChangePasswordTests : BaseIntegrationTest
{
    public ChangePasswordTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TEST START

    [Fact]
    public async Task ChangePassword_WhenValidData_ShouldReturn200()
    {
        var setupResult = await ChangePasswordSetup();

        var command = new ChangePasswordCommand(OldPassword: "Test123", NewPassword: "Test1234");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync("/api/profile/change-password", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "İstek sonucunda 200 kodu dönmeliydi.");

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Result boş olmamalı.");
        result.Code.Should().Be(Codes.PASSWORD_RESET_SUCCESS,
            "İstek sonucunda result içerisindeki code PASSWORD_RESET_SUCCESS olmalı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

            // validate refresh token in db

            var updatedRToken =
                await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == setupResult.RefreshToken);

            updatedRToken.Should().NotBeNull("Refresh token db de olmalı.");
            updatedRToken.IsRevoked.Should()
                .BeTrue("Parola değiştirme işlemi sonrası bulunan token geçersiz sayılmalıydı.");

            // validate refresh token remove in redis

            var userIdInRedis = await cacheService.GetAsync<string>(CacheKeys.RefreshToken(setupResult.RefreshToken));
            userIdInRedis.Should().BeNull("Refresh token redisten silinmeliydi.");

            // validate get me cache remove in redis

            var userInRedis =
                await cacheService.GetAsync<GetMeQueryResult>(CacheKeys.GetMeKey(setupResult.User.Id.ToString()));

            userInRedis.Should().BeNull("Get me için olan cache silinmeliydi.");

            // validate profile info cache remove in redis

            var profileInfoRedis =
                await cacheService.GetAsync<ProfileInfoQueryResult>(
                    CacheKeys.ProfileInfoKey(setupResult.User.Id.ToString()));

            profileInfoRedis.Should().BeNull("Profil bilgisi için olan cache silinmeliydi.");

            // validate change password

            var updatedUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == setupResult.User.Id);

            updatedUser.Should().NotBeNull();

            var oldPasswordCheck =
                passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, command.OldPassword);

            oldPasswordCheck.Should().Be(PasswordVerificationResult.Failed,
                "Parola değiştiği için eski parola ile karşılaştırma başarısız olmalıydı.");

            var newPasswordCheck =
                passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, command.NewPassword);

            newPasswordCheck.Should().Be(PasswordVerificationResult.Success,
                "Parola değiştiği için yeni parola ile hashlenen parola eşleşmeliydi.");
        }
    }

    // SUCCESS TEST END

    // ERROR TESTS START

    [Fact]
    public async Task ChangePassword_WhenUserUnauthorized_ShouldReturn401()
    {
        var command = new ChangePasswordCommand(OldPassword: "Test123", NewPassword: "Test1234");
        var response = await Client.PostAsJsonAsync("/api/profile/change-password", command);
        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized, "Kullanıcı yetkisiz olduğu için 401 kodu dönmeliydi.");
    }

    [Fact]
    public async Task ChangePassword_WhenUserNotExist_ShouldReturn404()
    {
        var setupResult = await ChangePasswordSetup();
        var command = new ChangePasswordCommand(OldPassword: "Test123", NewPassword: "Test1234");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == setupResult.RefreshToken);
            dbContext.RefreshTokens.Remove(token!);
            await dbContext.SaveChangesAsync();
            
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == setupResult.User.Id);
            dbContext.Users.Remove(user!);

            await dbContext.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/profile/change-password", command);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Kullanıcı bulunamadığıiçin 404 kodu dönmeliydi.");

        var result = await response.ReadContentAsAsync<NotFoundException>();

        result.Should().NotBeNull("Result null olmamalı.");
        result.Code.Should().Be(Codes.NOT_FOUND, "Result içerisindeki code NOT_FOUND olmalı.");
    }

    [Fact]
    public async Task ChangePasswordWhenValidationErrorsAcquired_ShouldReturn400()
    {
        var setupResult = await ChangePasswordSetup();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);
        var command = new ChangePasswordCommand(OldPassword: "", NewPassword: "Test123");
        var response = await Client.PostAsJsonAsync("/api/profile/change-password", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ERROR TESTS END

    // CHANGE PASSWORD SETUP
    private async Task<SetupResult> ChangePasswordSetup()
    {
        var setupResult = await CreateVanillaUserAsync();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginCommand(Email: setupResult.User.Email!, Password: "Test123"));

        var loginResult = await loginResponse.ReadContentAsAsync<LoginControllerResponse>();

        loginResult.Should().NotBeNull("Login result null gelmemeli.");

        var setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        setCookieHeader.Should().NotBeNull();

        var cookieValue = setCookieHeader.Split(';')[0];

        var pureRefreshToken = cookieValue.Split('=')[1];

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var currentTokenUserId = await cacheService.GetAsync<string>(CacheKeys.RefreshToken(pureRefreshToken));

            currentTokenUserId.Should().NotBeNull("User id refresh token ile redise yazılmalıydı.");
            currentTokenUserId.Should().Be(setupResult.User.Id.ToString(),
                "Redisteki user id ile setup içerisindeki userin id'si eşleşmeli.");
        }

        return new SetupResult
            { User = setupResult.User, AccessToken = loginResult.AccessToken!, RefreshToken = pureRefreshToken };
    }
}