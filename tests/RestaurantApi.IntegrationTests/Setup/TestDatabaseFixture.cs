using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using RestaurantApi.Application.Mail;
using RestaurantApi.Persistence.Extension;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace RestaurantApi.IntegrationTests.Setup;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("RestaurantTestDb")
        .WithUsername("postgres")
        .WithPassword("password123")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;
    private IConnectionMultiplexer _redisConnection = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _dbContainer.GetConnectionString());
    
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", $"{_redisContainer.GetConnectionString()},password=Secret123,allowAdmin=true");

        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "test-ortami-icin-en-az-32-karakterli-gizli-key-123!");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "restaurant-api");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "restaurant-api-client");
    
        Environment.SetEnvironmentVariable("MailSettings__Host", "smtp.gmail.com");
        Environment.SetEnvironmentVariable("MailSettings__Port", "587");
        Environment.SetEnvironmentVariable("MailSettings__Email", "test@gmail.com");
        Environment.SetEnvironmentVariable("MailSettings__Password", "secret");
    
        Environment.SetEnvironmentVariable("AWS__ServiceUrl", "http://localhost:4566");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => 
                        d.ServiceType == typeof(IMailService));
                    
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddScoped<IMailService, FakeMailService>();

                });
            });

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.Context.ApiContext>();

        await Factory.Services.SeedDataAsync();

        _redisConnection = ConnectionMultiplexer.Connect($"{_redisContainer.GetConnectionString()},allowAdmin=true");
    }

    public async Task ResetDataBaseAsync()
    {
        // respawner ilk kez çağırılıyorsa harita çıkarılsın
        if (_respawner == null)
        {
            _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                
                TablesToIgnore = new Respawn.Graph.Table[]
                {
                    "AspNetRoles",
                    "Permissions",    
                    "RolePermissions",
                    "__EFMigrationsHistory"
                }
            });
        }

        // db temizlesin
        await _respawner.ResetAsync(_dbConnection);
        
        await Factory.Services.SeedDataAsync();
        
        // redis temizlensin
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        await server.FlushAllDatabasesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbConnection != null) await _dbConnection.DisposeAsync();
        if (_redisConnection != null) await _redisConnection.DisposeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", null);

        await Factory.DisposeAsync();
        await Task.WhenAll(_dbContainer.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
    }
}