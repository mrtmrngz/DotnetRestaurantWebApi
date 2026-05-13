using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Infrastructure.Security.Authorization.Handler;
using RestaurantApi.Infrastructure.Security.Authorization.Requirements;

namespace RestaurantApi.Infrastructure.Security.Authorization.Extension;

public static class AuthorizationExtension
{
    public static IServiceCollection AddPermissionPolicy(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, RedisPermissionHandler>();
        
        services.AddAuthorization(options =>
        {
            var allPermissions = GetAllPermissions();

            foreach (var permission in allPermissions)
            {
                options.AddPolicy(permission, policy => 
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }
            
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
        });

        return services;
    }

    private static List<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        var nestedTypes = typeof(Permissions).GetNestedTypes();

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                        System.Reflection.BindingFlags.Static |
                                        System.Reflection.BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    var value = field.GetValue(null)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                        permissions.Add(value);
                }
            }
        }

        return permissions.Distinct().ToList();
    }
}