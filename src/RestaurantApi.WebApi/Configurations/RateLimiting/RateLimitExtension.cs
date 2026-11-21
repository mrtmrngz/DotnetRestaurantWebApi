using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.WebApi.Configurations.RateLimiting.Policies;
using RestaurantApi.WebApi.Configurations.RateLimiting.Settings;

namespace RestaurantApi.WebApi.Configurations.RateLimiting;

public static class RateLimitExtension
{
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // get service and validate limit > 0
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = configuration
            .GetSection(RateLimitSettings.SectionName)
            .Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ctx) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    code = Codes.TOO_MANY_REQUEST,
                    message = "Çok fazla istek gönderdiniz. Lütfen bir süre bekleyip tekrar deneyin."
                }, ctx);
            };

            // AUTH POLICY START

            options.RegisterPolicy(RateLimitPolicies.Auth.Register, settings.Enabled,
                settings.Auth.Register, useUserId: false);

            options.RegisterPolicy(RateLimitPolicies.Auth.General, settings.Enabled,
                settings.Auth.General, useUserId: false);

            options.RegisterPolicy(RateLimitPolicies.Auth.Refresh, settings.Enabled,
                settings.Auth.Refresh, useUserId: false);

            options.RegisterPolicy(RateLimitPolicies.Auth.MailVerify, settings.Enabled,
                settings.Auth.MailVerify, useUserId: false);

            // AUTH POLICY END

            // PASSWORD POLICY START

            options.RegisterPolicy(RateLimitPolicies.Password.ChangeVerify, settings.Enabled,
                settings.Password.ChangeVerify, useUserId: false);

            options.RegisterPolicy(RateLimitPolicies.Password.ChangeApply, settings.Enabled,
                settings.Password.ChangeApply, useUserId: false);

            // PASSWORD POLICY END

            // SAME USER POLICY START

            options.RegisterPolicy(RateLimitPolicies.SameUser.Standard, settings.Enabled,
                settings.SameUser.Standard);

            options.RegisterPolicy(RateLimitPolicies.SameUser.Strict, settings.Enabled,
                settings.SameUser.Strict);

            options.RegisterPolicy(RateLimitPolicies.SameUser.Restricted, settings.Enabled,
                settings.SameUser.Restricted);

            options.RegisterPolicy(RateLimitPolicies.SameUser.Critical, settings.Enabled,
                settings.SameUser.Critical);

            options.RegisterPolicy(RateLimitPolicies.SameUser.Flexible, settings.Enabled,
                settings.SameUser.Flexible);

            // SAME USER POLICY END

            // PANEL POLICY START

            options.RegisterPolicy(RateLimitPolicies.Panel.GeneralRead, settings.Enabled,
                settings.Panel.GeneralRead, perEndPoint: true);

            options.RegisterPolicy(RateLimitPolicies.Panel.Put, settings.Enabled,
                settings.Panel.Put);

            options.RegisterPolicy(RateLimitPolicies.Panel.GeneralPost, settings.Enabled,
                settings.Panel.GeneralPost);

            options.RegisterPolicy(RateLimitPolicies.Panel.GeneralPatch, settings.Enabled,
                settings.Panel.GeneralPatch);

            options.RegisterPolicy(RateLimitPolicies.Panel.GeneralDelete, settings.Enabled,
                settings.Panel.GeneralDelete);

            options.RegisterPolicy(RateLimitPolicies.Panel.StrictPatch, settings.Enabled,
                settings.Panel.StrictPatch);

            options.RegisterPolicy(RateLimitPolicies.Panel.FlexiblePatch, settings.Enabled,
                settings.Panel.FlexiblePatch);

            options.RegisterPolicy(RateLimitPolicies.Panel.Order, settings.Enabled,
                settings.Panel.Order);

            // PANEL POLICY END

            // CART POLICY START

            options.RegisterPolicy(RateLimitPolicies.Cart.GeneralCart, settings.Enabled,
                settings.Cart.GeneralCart);

            options.RegisterPolicy(RateLimitPolicies.Cart.UserRemoveCart, settings.Enabled,
                settings.Cart.UserRemoveCart);

            // CART POLICY END

            // CHECKOUT POLICY START

            options.RegisterPolicy(RateLimitPolicies.Checkout.User, settings.Enabled,
                settings.Checkout.User);

            options.RegisterPolicy(RateLimitPolicies.Checkout.HybridPanel, settings.Enabled,
                settings.Checkout.HybridPanel);

            // CHECKOUT POLICY END

            // CATEGORIES AND PUBLIC POLICIES START

            options.RegisterPolicy(RateLimitPolicies.Categories.GetCategories, settings.Enabled,
                settings.Categories.GetCategories, perEndPoint: true);

            options.RegisterPolicy(RateLimitPolicies.Public.Flexible, settings.Enabled,
                settings.Public.Flexible, perEndPoint: true);

            // CATEGORIES AND PUBLIC POLICIES END
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimitingConfiguration(
        this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static void RegisterPolicy(
        this RateLimiterOptions options,
        string policyName,
        bool enabled,
        RateLimitRule rule,
        bool useUserId = true,
        bool perEndPoint = false
    )
    {
        options.AddPolicy(policyName, ctx =>
            CreatePartition(ctx, policyName, enabled, rule, useUserId, perEndPoint));
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext ctx,
        string policyPrefix,
        bool enabled,
        RateLimitRule rule,
        bool useUserId = true,
        bool perEndpoint = false
    )
    {
        if (!enabled)
            return RateLimitPartition.GetNoLimiter("unlimited");

        string clientKey = useUserId
            ? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? GetClientIp(ctx)
            : GetClientIp(ctx);
        
        // GÜVENLİK: Request.Path yerine DisplayName kullanıyoruz çünkü path içinde dinamik parametreler (örn: /api/categories/{guid}) bulunur.
        // Request.Path kullanılsaydı saldırgan her istekte farklı bir GUID göndererek yeni partitionKey üretir ve limiti sınırsızca bypass ederdi.
        // DisplayName ise route şablonunu döner; parametre değişse bile tüm istekler aynı sayaçta toplanır.
        string endpointKey = perEndpoint
            ? $":{ctx.GetEndpoint()?.DisplayName ?? ctx.Request.Path.Value}"
            : string.Empty;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{policyPrefix}{endpointKey}:{clientKey}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rule.PermitLimit,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                QueueLimit = 0
            }
        );
    }

    private static string GetClientIp(HttpContext ctx)
    {
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}