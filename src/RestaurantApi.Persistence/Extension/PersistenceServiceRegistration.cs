using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Persistence.Context;
using RestaurantApi.Persistence.Identity;

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

        return services;
    }
}