using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Domain.Identity;
using RestaurantApi.Persistence.Context;
using RestaurantApi.Persistence.Repositories;

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
}