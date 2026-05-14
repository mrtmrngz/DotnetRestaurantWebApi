using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Identity;
using RestaurantApi.Persistence.Context;
using RestaurantApi.Persistence.Repositories;
using RestaurantApi.Persistence.Seed;

namespace RestaurantApi.Persistence.Extension;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddDbPersistance(this IServiceCollection services, IConfiguration config)
    {
        // API CONTEXT
        services.AddDbContext<ApiContext>(options =>
        {
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        });

        // IDENTITY
        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;

                options.User.RequireUniqueEmail = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
            })
            .AddEntityFrameworkStores<ApiContext>()
            .AddDefaultTokenProviders();
        
        // REPOSITORIES
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        return services;
    }

    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataSeeder");

        try
        {
            await context.Database.MigrateAsync();

            await PermissionSeeder.SeedPermissionAsync(context, logger);
            await RoleSeeder.SeedRolesAndPermissionsAsync(context, logger);
            logger.LogInformation("🏁 Tüm seed işlemleri başarıyla tamamlandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed datası yüklenirken bir hata oluştu!");
            throw;
        }
    }
}