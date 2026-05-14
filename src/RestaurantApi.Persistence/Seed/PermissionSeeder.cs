using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.Persistence.Seed;

public static class PermissionSeeder
{

    public static async Task SeedPermissionAsync(ApiContext context, ILogger logger)
    {
        
        logger.LogInformation("Permission seeding işlemi başladı.");
        
        var allPermissionInfo = GetAllPermissionsWithDetails();

        var existingPermissions = await context.Permissions.ToListAsync();

        var newPermissions = new List<Permission>();
        int updatedCount = 0;

        foreach (var info in allPermissionInfo)
        {
            var existing = existingPermissions.FirstOrDefault(p => p.Name == info.Name);
            if (existing == null)
            {
                newPermissions.Add(new Permission
                {
                    Name = info.Name,
                    Description = info.Description
                });
                logger.LogInformation("Yeni permission bulundu: {PermissionName}", info.Name);
            }else if (existing.Description != info.Description)
            {
                existing.Description = info.Description;
                updatedCount++;
                logger.LogInformation("Permission açıklaması güncellendi: {PermissionName}", info.Name);
            }
        }

        if (newPermissions.Any())
        {
            await context.Permissions.AddRangeAsync(newPermissions);
            logger.LogInformation("{Count} adet yeni permission veritabanına ekleniyor...", newPermissions.Count);
        }

        if (updatedCount > 0)
            logger.LogInformation("{Count} adet permission açıklaması güncellendi.", updatedCount);
        
        await context.SaveChangesAsync();
        logger.LogInformation("Permission seeding işlemi başarıyla tamamlandı.");
    }

    private static List<(string Name, string Description)> GetAllPermissionsWithDetails()
    {
        var permissions = new List<(string Name, string Description)>();
        var permissionClass = typeof(Domain.Constants.Permissions);
        var nestedTypes = permissionClass.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    var name = field.GetValue(null)?.ToString();
                    var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();
                    var description = descriptionAttribute?.Description ?? $"{name} yetkisi";

                    if (!string.IsNullOrEmpty(name))
                    {
                        permissions.Add((name, description));
                    }
                }
            }
        }

        return permissions;
    }
}