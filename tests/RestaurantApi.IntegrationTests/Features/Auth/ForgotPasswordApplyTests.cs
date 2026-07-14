using System.Net;
using System.Net.Http.Json;
using Amazon.Runtime.Internal;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordApply;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class ForgotPasswordSetupResult
{
    public AppUser User { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class ForgotPasswordApplyTests : BaseIntegrationTest
{
    public ForgotPasswordApplyTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TEST START

    [Fact]
    public async Task ForgotPasswordApply_WhenValidData_ShouldReturnOK()
    {
        var oldPass = "Test1234";
        var newPass = "NewTest1234";

        var setupResult = await ForgotPasswordSetup(oldPass);

        var command = new ForgotPasswordApplyCommand(Token: setupResult.Token, Password: newPass);

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/apply", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull();
        result.Code.Should().Be(Codes.PASSWORD_RESET_SUCCESS);

        // CHECK PASSWORD CHANGE
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

            var updatedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == setupResult.User.Id);

            updatedUser.Should().NotBeNull("User veritabanında bulunmalı.");

            var oldPassCheck = passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, oldPass);

            oldPassCheck.Should().Be(
                PasswordVerificationResult.Failed,
                "Kullanıcının eski parolası ile şuan dbde hashlenmiş parolası eşleşmemeli.");

            var newPassCheck = passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, newPass);

            newPassCheck.Should().Be(
                PasswordVerificationResult.Success,
                "Kullanıcının yeni parolası ile şuan dbde hashlenmiş parolası eşleşmeli.");
        }

        // CHECK CACHE
        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var cacheKey = CacheKeys.ForgotPasswordKey(setupResult.Token);

            var existOnCache = await cacheService.GetAsync<string>(cacheKey);

            existOnCache.Should().BeNull("Token redisten silinmiş olmalı.");
        }
    }

    // SUCCESS TEST END

    // ERROR TESTS START

    [Fact]
    public async Task ForgotPasswordApply_WhenUserIdNotInCache_ShouldReturnUnauthorized()
    {
        var command = new ForgotPasswordApplyCommand(Token: Guid.NewGuid().ToString(), Password: "Test123");

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/apply", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.ReadContentAsAsync<ErrorResponse>();

        result.Should().NotBeNull("Result boş olmamalı.");
        result.Code.Should().Be(Codes.UNAUTHORIZED.ToString(), "Result içerisindeki code NOT_FOUND olmalı.");
        result.Message.Should()
            .Be("Parola değiştirmek için size sağlanan token süresi sona erdi, lütfen tekrar deneyiniz.");
    }

    [Fact]
    public async Task ForgotPasswordApply_WhenUserNotInDB_ShouldReturnNotFound()
    {
        var oldPass = "Test1234";
        var newPass = "NewTest1234";

        var setupResult = await ForgotPasswordSetup(oldPass);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            context.Users.Remove(setupResult.User);
            await context.SaveChangesAsync();
        }

        var command = new ForgotPasswordApplyCommand(Token: setupResult.Token, Password: newPass);

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/apply", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var result = await response.ReadContentAsAsync<ErrorResponse>();

        result.Should().NotBeNull("Result boş olmamalı.");
        result.Code.Should().Be(Codes.NOT_FOUND.ToString(), "Result içerisindeki code NOT_FOUND olmalı.");
    }

    [Fact]
    public async Task ForgotPasswordApply_WhenPasswordDoesNotMeetRequirements_ShouldReturn400BadRequest()
    {
        var oldPass = "Test1234";
        var newPass = "1234";

        var setupResult = await ForgotPasswordSetup(oldPass);

        var command = new ForgotPasswordApplyCommand(Token: setupResult.Token, Password: newPass);

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/apply", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Şifre kurallara uymadığı için 400 dönmeli.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

            var updatedUser = await context.Users.FirstOrDefaultAsync(x => x.Id == setupResult.User.Id);
            updatedUser.Should().NotBeNull("Kullanıcı DB'de duruyor olmalı.");

            var oldPassCheck = passwordHasher.VerifyHashedPassword(updatedUser, updatedUser.PasswordHash!, oldPass);

            oldPassCheck.Should().Be(
                PasswordVerificationResult.Success,
                "Şifre kurallara uymadığı için veritabanındaki eski şifre aynen korunmuş olmalı."
            );
        }
    }

    // ERROR TESTS END

    // SETUP

    private async Task<ForgotPasswordSetupResult> ForgotPasswordSetup(string oldPass)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var email = "john@gmail.com";
        var userModel = new AppUser
        {
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            Name = "John",
            Surname = "Doe",
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        userModel.PasswordHash = passwordHasher.HashPassword(userModel, oldPass);

        context.Users.Add(userModel);
        await context.SaveChangesAsync();

        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(await userManager.GeneratePasswordResetTokenAsync(userModel))
        );

        await cacheService.SetAsync(CacheKeys.ForgotPasswordKey(token), userModel.Id.ToString(),
            TimeSpan.FromMinutes(15));

        return new ForgotPasswordSetupResult { User = userModel, Token = token };
    }
}