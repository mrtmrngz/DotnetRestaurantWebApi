using RestaurantApi.Infrastructure.DependencyInjection;
using RestaurantApi.Persistence.Extension;
using RestaurantApi.WebApi.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(builder.Configuration.GetValue<string>("ConnectionStrings:Redis"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddOpenApi();

// Database & Identity configuration
builder.Services.AddDbPersistance(builder.Configuration);

// Redis Configuration
builder.Services.AddRedis(builder.Configuration);

// INFRA DI REGISTRATION
builder.Services.AddInfrastructureServices();

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Logging Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();