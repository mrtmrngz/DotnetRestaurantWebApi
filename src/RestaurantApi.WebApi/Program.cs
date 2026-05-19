using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using RestaurantApi.Application.Common.Extensions;
using RestaurantApi.Infrastructure.DependencyInjection;
using RestaurantApi.Persistence.Extension;
using RestaurantApi.WebApi.Middlewares;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// Console.WriteLine(builder.Configuration.GetValue<string>("ConnectionStrings:Redis"));
// Console.WriteLine(builder.Configuration.GetValue<string>("JwtSettings:SecretKey"));
// Console.WriteLine(builder.Configuration.GetValue<string>("MailSettings:Host"));
// Console.WriteLine(builder.Configuration.GetValue<string>("AWS:ServiceUrl"));
// Console.WriteLine(builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo{Title = "Restaurant Api", Version = "v1"});
    c.ExampleFilters();
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT tokeninizi giriniz."
    });
    
    c.AddSecurityRequirement((doc) => 
    {
        var requirement = new OpenApiSecurityRequirement();
        var schemeReference = new OpenApiSecuritySchemeReference("Bearer", doc);
        
        requirement.Add(schemeReference, new List<string>());
        
        return requirement;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant api v1");
    });
}

app.UseHttpsRedirection();

// Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Logging Middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard();

await app.Services.SeedDataAsync();

app.Run();