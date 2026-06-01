using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.MailVerify;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class CreateUserAndReturnUserAndTokenDto
{
    public string Token { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}

public class MailVerifyTests : BaseIntegrationTest
{
    public MailVerifyTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START
    [Fact]
    public async Task MailVerifyAsync_ShouldReturnSuccess_WhenValidData()
    {
        // seed db
        CreateUserAndReturnUserAndTokenDto modelResult = await CreateUserAndReturnUserAndToken();

        var command = new MailVerifyCommand(modelResult.Token);

        var response = await Client.PostAsJsonAsync("/api/auth/mail-verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        bool isWelcomeMailEnqueued = false;
        var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();

        for (int i = 0; i < 5; i++)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100);
            var processingJobs = monitoringApi.ProcessingJobs(0, 100);
            var succeededJobs = monitoringApi.SucceededJobs(0, 100);

            var allJobs = enqueuedJobs.Select(j => j.Value.Job)
                .Concat(processingJobs.Select(j => j.Value.Job))
                .Concat(succeededJobs.Select(j => j.Value.Job));

            var targetJob = allJobs.FirstOrDefault(job =>
                job.Method.Name == "ExecuteWelcomeMailAsync" &&
                job.Args.Contains(modelResult.User.Email));

            if (targetJob != null)
            {
                isWelcomeMailEnqueued = true;

                var mailModel = targetJob.Args.FirstOrDefault(x => x is WelcomeMailViewModel) as WelcomeMailViewModel;

                if (mailModel != null)
                {
                    mailModel.CustomerName.Should().Be("John Doe");
                }

                break;
            }

            await Task.Delay(100);
        }

        isWelcomeMailEnqueued.Should()
            .BeTrue("Event tetiklendikten sonra Hangfire üzerinde Hoş Geldin mail işi oluşturulmalıydı!");

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>(jsonOptions);
        result!.Code.Should().Be(Codes.MAIL_VERIFIED_SUCCESS);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var user = await context.Users.FindAsync(modelResult.User.Id);
            user.Should().NotBeNull();
            user.EmailConfirmed.Should().BeTrue();

            var cachedData = await cacheService.GetAsync<string>(CacheKeys.MailVerificationToken(modelResult.Token));
            cachedData.Should().BeNull();
        }
    }

    // SUCCESS TESTS START

    // ERROR TESTS START

    [Fact]
    public async Task MailVerifiedAsync_ShouldReturn400_WhenMailVerificationFailed()
    {
        CreateUserAndReturnUserAndTokenDto modelResult = await CreateUserAndReturnUserAndToken();
        
        string validFormatButWrongToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes("bu-tamamen-gecersiz-ve-sahte-bir-tokendir-kral")
        );

        using (var scope = Factory.Services.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await redis.SetAsync(CacheKeys.MailVerificationToken(validFormatButWrongToken),
                modelResult.User.Id, TimeSpan.FromMinutes(5));
        }

        var invalidCommand = new MailVerifyCommand(validFormatButWrongToken);
        
        var response = await Client.PostAsJsonAsync("/api/auth/mail-verify", invalidCommand);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var user = await context.Users.FindAsync(modelResult.User.Id);
            user.Should().NotBeNull("User null gelmemeliydi.");
            user.EmailConfirmed.Should().BeFalse("Geçersiz token gönderildiği için kullanıcı doğrulanmamalıydı.");
        }
    }

    [Fact]
    public async Task MailVerifiedAsync_ShouldReturn409_WhenUserAlreadyVerified()
    {
        CreateUserAndReturnUserAndTokenDto modelResult = await CreateUserAndReturnUserAndToken(u => u.EmailConfirmed = true);
        
        var command = new MailVerifyCommand(modelResult.Token);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var user = await context.Users.FindAsync(modelResult.User.Id);

            user.Should().NotBeNull("Kullanıcı null gelmemeliydi");
            user.EmailConfirmed.Should().BeTrue("Kullanıcı oluşturulrken mail doğrulandığı için true gelmeliydi.");
        }

        var response = await Client.PostAsJsonAsync("/api/auth/mail-verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MailVerifiedAsync_ShouldReturn404_WhenUserNotExist()
    {
        CreateUserAndReturnUserAndTokenDto modelResult = await CreateUserAndReturnUserAndToken();

        var command = new MailVerifyCommand(modelResult.Token);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            context.Users.Remove(modelResult.User);
            await context.SaveChangesAsync();
        }
        
        var response = await Client.PostAsJsonAsync("/api/auth/mail-verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task MailVerifiedAsync_ShouldReturn403_WhenInvalidToken()
    {
        CreateUserAndReturnUserAndTokenDto modelResult = await CreateUserAndReturnUserAndToken();

        var command = new MailVerifyCommand(modelResult.Token);

        using (var scope = Factory.Services.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<ICacheService>();

            await redis.RemoveAsync(CacheKeys.MailVerificationToken(modelResult.Token));
        }
        
        var response = await Client.PostAsJsonAsync("/api/auth/mail-verify", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    // ERROR TESTS END

    // Seed Data
    private async Task<CreateUserAndReturnUserAndTokenDto> CreateUserAndReturnUserAndToken(Action<AppUser>? customConfig = null)
    {
        AppUser user;
        string token;

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

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
            userModel.PasswordHash = passwordHasher.HashPassword(userModel, "Test123");
            
            customConfig?.Invoke(userModel);

            context.Users.Add(userModel);
            await context.SaveChangesAsync();

            user = userModel;
            token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes(await userManager.GenerateEmailConfirmationTokenAsync(userModel))
            );

            await cacheService.SetAsync(CacheKeys.MailVerificationToken(token), user.Id, TimeSpan.FromMinutes(5));
        }

        return new CreateUserAndReturnUserAndTokenDto() { User = user, Token = token };
    }
}