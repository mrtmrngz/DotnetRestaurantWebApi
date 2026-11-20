namespace RestaurantApi.WebApi.Configurations.Cors;

public static class CorsExtension
{
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {

        services.AddOptions<CorsSettings>()
            .BindConfiguration(CorsSettings.SectionName)
            .Validate(settings => settings.AllowedOrigins.Length > 0,
                "Cors:AllowedOrigins boş bırakılamaz. Geliştirilme ortamı için en azından (*) tanımlanmalıdır.")
            .Validate(settings => environment.IsDevelopment() || !settings.AllowedOrigins.Contains("*"),
                "Güvenlik gereği production ortamında her rotanın (*) erişilmesine izin verilemez.")
            .ValidateOnStart();
        
        var corsSettings = configuration
            .GetSection(CorsSettings.SectionName)
            .Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsSettings.PolicyName, policy =>
            {
                if (corsSettings.AllowedOrigins.Contains("*"))
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
                else
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
            });
        });

        return services;
    }
    
    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app)
    {
        return app.UseCors(CorsSettings.PolicyName);
    }
}