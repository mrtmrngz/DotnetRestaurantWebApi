using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Auth.Commands.Register;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Auth;

public class RegisterTests : BaseIntegrationTest
{
    public RegisterTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Register_WithValidCommand_ShouldCreateUserInDatabaseAndReturnSuccess()
    {
        var command = new RegisterCommand(
            Name: "John",
            Surname: "Doe",
            Email: "john@mail.com",
            Password: "Secret123",
            PhoneNumber: "+905441234567"
        );

        var response = await Client.PostAsJsonAsync("api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var monitoringApi = Hangfire.JobStorage.Current.GetMonitoringApi();
        bool isJobEnqueued = false;

        for (int i = 0; i < 5; i++)
        {
            var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 100);
        
            var processingJobs = monitoringApi.ProcessingJobs(0, 100);
        
            var succeededJobs = monitoringApi.SucceededJobs(0, 100);

            isJobEnqueued = enqueuedJobs.Any(j => j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" && j.Value.Job.Args.Contains(command.Email)) ||
                            processingJobs.Any(j => j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" && j.Value.Job.Args.Contains(command.Email)) ||
                            succeededJobs.Any(j => j.Value.Job.Method.Name == "ExecuteMailVerifyMailAsync" && j.Value.Job.Args.Contains(command.Email));

            if (isJobEnqueued) break;

            await Task.Delay(100);
        }

        isJobEnqueued.Should().BeTrue("Kullanıcı kaydolduğunda Hangfire arka plan mail işi oluşturmalı ve işlemeliydi!");
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>(jsonOptions);

        result.Should().NotBeNull();
        result!.Message.Should().Be("Kayıt işlemi başarılı, mail adresinize doğrulama bağlantısı gönderildi.");
        result!.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS);

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var userInDb = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == command.Email);

            userInDb.Should().NotBeNull();
            userInDb!.Name.Should().Be(command.Name);
            userInDb!.Surname.Should().Be(command.Surname);
        }
    }

    [Fact]
    public async Task Register_WhenUserExistSameEmail_ShouldReturnConflictException()
    {
        var command = new RegisterCommand(
            Name: "John",
            Surname: "Doe",
            Email: "john@mail.com",
            Password: "Secret123",
            PhoneNumber: "+905441234567"
        );

        await Client.PostAsJsonAsync("api/auth/register", command);
        var response = await Client.PostAsJsonAsync("api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<BaseResponse>(jsonOptions);
        
        result.Should().NotBeNull();
        result!.Code.Should().Be(Codes.CONFLICT);
        
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var userCount = await dbContext.Users.CountAsync(u => u.Email == command.Email);
        
            // Veritabanında bu maille SADECE 1 kullanıcı olmalı, ikinciyi eklememiş olmalı!
            userCount.Should().Be(1); 
        }
    }

    [Fact]
    public async Task Register_WhenInvalidCommand_ShouldThrowValidationException()
    {
        var command = new RegisterCommand(
            Name: "John",
            Surname: "Doe",
            Email: "john@mail.com",
            Password: "test",
            PhoneNumber: "+905441234567"
        );

        var response = await Client.PostAsJsonAsync("api/auth/register", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WhenRoleAssignmentFails_ShouldRollbackTransactionAndNotSaveUser()
    {
        var command = new RegisterCommand(
            Name: "John",
            Surname: "Doe",
            Email: "john@mail.com",
            Password: "Secret123",
            PhoneNumber: "+905441234567"
        );

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.User);
            if (userRole is not null)
            {
                dbContext.Roles.Remove(userRole);
                await dbContext.SaveChangesAsync();
            }
        }
        
        var response = await Client.PostAsJsonAsync("api/auth/register", command);
        
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
            var existUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == command.Email);
            existUser.Should().BeNull("Rol ataması patladığı için transaction geri alınmalı ve kullanıcı kaydedilmemeliydi!");
        }
    }
}