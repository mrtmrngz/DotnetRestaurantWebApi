using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
}