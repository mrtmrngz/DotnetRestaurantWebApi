using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Mail;
using RestaurantApi.Infrastructure.Auth;
using RestaurantApi.Infrastructure.Cache;
using RestaurantApi.Infrastructure.Mail;
using RestaurantApi.Infrastructure.Mail.Factory;
using RestaurantApi.Infrastructure.Mail.Handlers;
using RestaurantApi.Infrastructure.Security.Authorization.Extension;
using RestaurantApi.Infrastructure.Settings;
using RestaurantApi.Infrastructure.Storage;

namespace RestaurantApi.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RedisLockService>();
        services.AddScoped<ICacheService, CacheService>();

        services.AddHttpContextAccessor();
        
        // s3
        services.Configure<AwsSettings>(configuration.GetSection("AWS"));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var aws = sp.GetRequiredService<IOptions<AwsSettings>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = aws.ServiceUrl,
                ForcePathStyle = true,
                RegionEndpoint = RegionEndpoint.GetBySystemName(aws.Region)
            };

            return new AmazonS3Client(
                aws.AccessKey,
                aws.SecretKey,
                config
            );
        });

        services.AddScoped<IFileStorage, S3FileStorage>();

        services.AddScoped<IGenerateRefreshToken, RefreshTokenService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddJwtAuthenticationConfiguration(configuration);
        services.AddPermissionPolicy();
        
        // mail service secrets
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        
        // mail service
        services.AddScoped<IMailService, SmtpMailService>();
        services.AddSingleton<IMailTemplateRenderer, RazorTemplateRenderer>();
        
        // mail handlers
        services.AddScoped<IMailHandler, WelcomeMailHandler>();
        
        // mail factory
        services.AddScoped<IMailFactory, MailFactory>();

        return services;
    }    
}