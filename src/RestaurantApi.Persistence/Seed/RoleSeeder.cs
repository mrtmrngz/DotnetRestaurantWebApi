
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Seed;

public static class RoleSeeder
{
    public static async Task SeedRolesAndPermissionsAsync(ApiContext context, ILogger logger)
    {
        logger.LogInformation("🛡️ Rol ve Yetki eşleştirme işlemi başlatılıyor...");

        var roleNames = GetAllRolesFromConstants();

        foreach (var roleName in roleNames)
        {
            var normalizedName = roleName.ToUpperInvariant();
            var roleExist = await context.Roles.AnyAsync(r => r.NormalizedName == normalizedName);

            if (!roleExist)
            {
                logger.LogInformation("🎭 Yeni rol oluşturuluyor: {RoleName}", roleName);

                await context.Roles.AddAsync(new AppRole
                {
                    Name = roleName,
                    NormalizedName = normalizedName,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }
        }

        await context.SaveChangesAsync();
        await AssignAllPermissionsToAdmin(context, logger);
    }

    private static List<string> GetAllRolesFromConstants()
    {
        return typeof(AppRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .Cast<string>()
            .ToList();
    }

    private static async Task AssignAllPermissionsToAdmin(ApiContext context, ILogger logger)
    {
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Admin);
        if(adminRole == null) return;

        var allPermission = await context.Permissions.ToListAsync();

        var existingRolePermissions = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var newRolePermissions = new List<RolePermission>();

        foreach (var permission in allPermission)
        {
            if (!existingRolePermissions.Contains(permission.Id))
            {
                newRolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
        }

        if (newRolePermissions.Any())
        {
            await context.RolePermissions.AddRangeAsync(newRolePermissions);
            await context.SaveChangesAsync();
            logger.LogInformation("👑 Admin rolüne {Count} adet yeni yetki tanımlandı.", newRolePermissions.Count);
        }
        else
        {
            logger.LogInformation("✅ Admin rolü zaten tüm yetkilere sahip.");
        }
    }
}