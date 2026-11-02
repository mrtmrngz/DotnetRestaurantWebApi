using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common.Abstractions.Settings;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Domain.Identity;

namespace RestaurantApi.Persistence.Seed;

public static class RootAdminSeeder
{
    public static async Task SeedRootAdminAsync(
        UserManager<AppUser> userManager,
        RootAdminSettings adminSettings,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(adminSettings.Email) || string.IsNullOrWhiteSpace(adminSettings.Password))
        {
            logger.LogWarning("⚠️ RootAdmin credentials bulunamadı (UserSecrets/AppConfig). Root Admin seed adımı atlandı.");
            return;
        }   
        
        logger.LogInformation("👑 Root Admin seeder kontrolü başlatılıyor...");

        var existingAdmin = await userManager.FindByEmailAsync(adminSettings.Email);

        if (existingAdmin == null)
        {
            var rootAdmin = new AppUser
            {
                Name = "Admin",
                Surname = "Admin",
                UserName = adminSettings.Email,
                NormalizedUserName = adminSettings.Email.ToUpper(),
                Email = adminSettings.Email,
                NormalizedEmail = adminSettings.Email.ToUpper(),
                PhoneNumber = adminSettings.PhoneNumber,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            
            var createResult = await userManager.CreateAsync(rootAdmin, adminSettings.Password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(rootAdmin, AppRoles.Admin);
                logger.LogInformation("👑 Root Admin hesabı oluşturuldu ve ADMIN rolü atandı: {Email}", adminSettings.Email);
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("❌ Root Admin oluşturulamadı: {Errors}", errors);
            }
        }
        else if (!await userManager.IsInRoleAsync(existingAdmin, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(existingAdmin, AppRoles.Admin);
            logger.LogInformation("ℹ️ Mevcut {Email} kullanıcısına ADMIN rolü eklendi.", adminSettings.Email);
        }
        else
        {
            logger.LogInformation("Admin veri tabanında mevcut olduğu için seed işlemi yapılmadı.");
        }
    }
}