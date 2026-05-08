using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Persistence.Context;
using RestaurantApi.Persistence.Extension;
using RestaurantApi.Persistence.Identity;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();