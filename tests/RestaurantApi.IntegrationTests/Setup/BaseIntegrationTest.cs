using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Features.Users;
using RestaurantApi.Persistence.Context;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace RestaurantApi.IntegrationTests.Setup;

[Collection("DatabaseCollection")]
public class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly TestDatabaseFixture Fixture;
    protected readonly HttpClient Client;
    protected readonly WebApplicationFactory<Program> Factory;

    public BaseIntegrationTest(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
        Factory = fixture.Factory;
        Client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Fixture.ResetDataBaseAsync();
    }
    
    protected async Task<VanillaUserSetupDto> CreateVanillaUserAsync(Action<AppUser>? customConfig = null)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

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

        customConfig?.Invoke(user);
        
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var roles = new List<string> { "USER" };
        var accessToken = await jwtTokenService.CreateAccessToken(user, roles);

        return new VanillaUserSetupDto { User = user, AccessToken = accessToken };
    }
}