using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Application.Common.Extensions;
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
builder.Services.AddInfrastructureServices(builder.Configuration);

// MEDIATR & FLUENT VALIDATION PIPELINE
builder.Services.AddApplicationServices();

// suppress model state validation
builder.Services.Configure<ApiBehaviorOptions>(opts =>
{
    opts.SuppressModelStateInvalidFilter = true;
});

// lower case endpoint
builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.Enrich.FromLogContext()
        .WriteTo.Console(
            theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Seq("http://localhost:5341");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Logging Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.Services.SeedDataAsync();

app.Run();