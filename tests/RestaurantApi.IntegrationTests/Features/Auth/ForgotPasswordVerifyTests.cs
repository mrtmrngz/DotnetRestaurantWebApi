using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Auth.Commands.ForgotPasswordVerify;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class ForgotPasswordVerifyTests : BaseIntegrationTest
{
    public ForgotPasswordVerifyTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task ForgotPasswordVerify_WhenValidData_ShouldReturnSuccess()
    {
        var user = await SetupForForgotPassword();

        var command = new ForgotPasswordVerifyCommand(Email: user.Email!);

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string? extractedToken = null;
        var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();
        bool isJobEnqueued = false;
        string targetEmail = command.Email;

        for (int i = 0; i < 5; i++)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100);
            var processingJobs = monitoringApi.ProcessingJobs(0, 100);
            var succeededJobs = monitoringApi.SucceededJobs(0, 100);

            var allJobs = enqueuedJobs.Select(j => j.Value.Job)
                .Concat(processingJobs.Select(j => j.Value.Job))
                .Concat(succeededJobs.Select(j => j.Value.Job));

            var targetJob = allJobs.FirstOrDefault(job =>
                job.Method.Name == "ExecuteForgotPasswordMailAsync" &&
                job.Args.Contains(targetEmail));

            if (targetJob != null)
            {
                isJobEnqueued = true;

                var mailViewModel = targetJob.Args[1] as ForgotPasswordViewModel;

                if (mailViewModel != null)
                {
                    var uri = new Uri(mailViewModel.ResetLink);
                    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

                    if (query.TryGetValue("token", out var tokenValue))
                    {
                        extractedToken = tokenValue.ToString();
                    }
                }

                break;
            }

            await Task.Delay(100);
        }

        isJobEnqueued.Should()
            .BeTrue(
                "Şifre sıfırlama talebinde Hangfire arka planda 'ExecuteForgotPasswordMailAsync' işini oluşturmalıydı!");
        extractedToken.Should()
            .NotBeNullOrEmpty("Hangfire işinin içerisindeki modelden Reset Token başarıyla ayıklanmalıydı!");

        using (var scope = Factory.Services.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cacheKey = CacheKeys.ForgotPasswordKey(extractedToken);
            var userId = await cacheService.GetAsync<string>(cacheKey);

            userId.Should().NotBeNull("User id null olmamalı.");
            userId.Should().Be(user.Id.ToString(), "User id oluşturduğumuz kullanıcı id'si ile eşleşmeli.");
        }
    }

    [Fact]
    public async Task ForgotPasswordVerify_WhenUserNotExist_ShouldReturnSuccess()
    {
        var command = new ForgotPasswordVerifyCommand(Email: "someinvalid@mail.com");

        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // SUCCESS TESTS END
    
    // ERROR TESTS START

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalidmail")]
    [InlineData("invalidmail.com")]
    public async Task ForgotPasswordVerify_WhenInvalidData_ShouldReturnValidationException(string invalidEmail)
    {
        var command = new ForgotPasswordVerifyCommand(Email: invalidEmail);
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password/verify", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadContentAsAsync<ValidationErrorResponse>();

        result.Should().NotBeNull("Result boş olmamalı.");
        result.Code.Should().Be(Codes.VALIDATION_ERROR);
    }
    
    // ERROR TESTS END

    // SETUP
    private async Task<AppUser> SetupForForgotPassword()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();

        var email = $"test_{Guid.NewGuid().ToString()[..8]}@gmail.com";
        var password = "Test123";
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

        dbContext.Entry(user).State = EntityState.Detached;

        return user;
    }
}