using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.Login;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class LoginTests : BaseIntegrationTest
{
    public LoginTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task LoginAsync_WhenValidDataAndUserVerifiedAndTwoFactorDisable_ShouldReturnSuccess()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, command.Password);

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNull();
        result.Code.Should().Be(Codes.LOGIN_SUCCESS);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie");
        setCookieHeader.Should().NotBeNull("Giriş başarılı olduğunda Set-Cookie header'ı dönmeliydi.");

        var sessionCookie = setCookieHeader.FirstOrDefault(c => c.StartsWith("_session"));
        sessionCookie.Should().NotBeNull("Dönen cookie'lerin arasında '_session' adında bir cookie bulunmalı.");

        var rawCookieValue = sessionCookie!.Split(';')[0];
        var refreshTokenFromCookie = rawCookieValue.Split('=')[1];

        refreshTokenFromCookie.Should().NotBeNullOrEmpty("Cookie içerisindeki refresh token boş olamaz.");
        sessionCookie.Should().Contain("httponly", "Güvenlik gereği refresh token cookie'si HttpOnly olmalıdır!");

        using (var scope = Factory.Services.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var userId = await redis.GetAsync<string>(CacheKeys.RefreshToken(refreshTokenFromCookie));
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            userId.Should().NotBeNull();
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId));
            user.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task LoginAsync_WhenValidDataAndUserTwoFactorEnable_ShouldReturnSuccessAndShouldSendEmailNotification()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, command.Password, u => u.TwoFactorEnabled = true);

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string? extractedOtp = null;
        var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();
        bool isJobEnqueued = false;

        for (int i = 0; i < 5; i++)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100);
            var processingJobs = monitoringApi.ProcessingJobs(0, 100);
            var succeededJobs = monitoringApi.SucceededJobs(0, 100);

            var allJobs = enqueuedJobs.Select(j => j.Value.Job)
                .Concat(processingJobs.Select(j => j.Value.Job))
                .Concat(succeededJobs.Select(j => j.Value.Job));

            var targetJob = allJobs.FirstOrDefault(job =>
                job.Method.Name == "ExecuteOtpMailAsync" &&
                job.Args.Contains(command.Email));

            if (targetJob != null)
            {
                isJobEnqueued = true;
                var mailViewModel = targetJob.Args[1] as OtpMailViewModel;

                if (mailViewModel != null)
                {
                    extractedOtp = mailViewModel.OtpCode;
                }
                else
                {
                    dynamic dynamicModel = targetJob.Args[1];
                    extractedOtp = dynamicModel?.Code?.ToString();
                }

                break;
            }

            await Task.Delay(100);
        }

        isJobEnqueued.Should().BeTrue("Kullanıcı 2fa açıksa Hangfire arka plan mail işi oluşturmalıydı!");
        extractedOtp.Should()
            .NotBeNullOrEmpty("Hangfire işinin içerisindeki modelden OTP kodu başarıyla ayıklanmalıydı!");

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.AccessToken.Should().BeNull();
        result.Code.Should().Be(Codes.TWO_FACTOR_REQUIRED);

        using (var scope = Factory.Services.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var key = CacheKeys.OtpToken(extractedOtp, "twoFactorAuth");
            var userId = await redis.GetAsync<string>(key);

            userId.Should().NotBeNull("User id redis içerisinde olması gerekir.");
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId));

            user.Should().NotBeNull("Eğer redis içerisinde user varsa burası null dönemmeli");
        }
    }

    [Fact]
    public async Task LoginAsync_WhenValidDataButUserMailNotVerified_ShouldReturnSuccessAndShouldSendEmailNotification()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, command.Password, u => u.EmailConfirmed = false);

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();
        bool isJobEnqueued = false;

        for (int i = 0; i < 5; i++)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100);

            var processingJobs = monitoringApi.ProcessingJobs(0, 100);

            var succeededJobs = monitoringApi.SucceededJobs(0, 100);

            isJobEnqueued = enqueuedJobs.Any(j =>
                                j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" &&
                                j.Value.Job.Args.Contains(command.Email)) ||
                            processingJobs.Any(j =>
                                j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" &&
                                j.Value.Job.Args.Contains(command.Email)) ||
                            succeededJobs.Any(j =>
                                j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" &&
                                j.Value.Job.Args.Contains(command.Email));

            if (isJobEnqueued) break;

            await Task.Delay(100);
        }

        isJobEnqueued.Should()
            .BeTrue(
                "Kullanıcının mail adresi doğrulanmamış ise Hangfire arka plan mail işi oluşturmalı ve işlemeliydi!");

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions);

        result.Should().NotBeNull();
        result.Code.Should().Be(Codes.MAIL_VERIFICATION_REQUIRED);
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task LoginAsync_WhenValidDataButUserNotExist_ShouldReturnUnauthorizedException()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );
        var response = await Client.PostAsJsonAsync("/api/auth/login", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        LoginAsync_WhenValidDataButUserLockEnabledAndLockExpireTimeLTCurrentTime_ShouldReturnForbiddenException()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, command.Password, u =>
        {
            u.LockoutEnabled = true;
            u.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsLockedOut_ShouldReturnForbidden()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, command.Password, u =>
        {
            u.LockoutEnabled = true;
            u.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
            u.AccessFailedCount = 5;
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidPassword_ShouldReturnUnauthorized()
    {
        var command = new LoginCommand(
            Email: "test@mail.com",
            Password: "Secret123"
        );

        await SeedUserAsync(command.Email, "invalid-password");

        var response = await Client.PostAsJsonAsync("/api/auth/login", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ERROR TESTS END

    private async Task<AppUser> SeedUserAsync(string email, string password, Action<AppUser>? customConfig = null)
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

        // 🎯 Eğer testlerin içinde TwoFactorEnabled veya LockoutEnd gibi özel alanlar geçilecekse dynamic besliyoruz
        customConfig?.Invoke(user);

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}