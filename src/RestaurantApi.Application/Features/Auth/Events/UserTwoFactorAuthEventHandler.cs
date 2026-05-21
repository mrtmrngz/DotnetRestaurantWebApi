using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Auth.Events;

public class UserTwoFactorAuthEventHandler : INotificationHandler<UserTwoFactorAuthEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<UserTwoFactorAuthEventHandler> _logger;
    private readonly IMailFactory _mailFactory;

    public UserTwoFactorAuthEventHandler(ICacheService cacheService, ILogger<UserTwoFactorAuthEventHandler> logger, IMailFactory mailFactory)
    {
        _cacheService = cacheService;
        _logger = logger;
        _mailFactory = mailFactory;
    }

    public async Task Handle(UserTwoFactorAuthEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "📢 UserTwoFactorAuthEvent yakalandı! Mail ve Cache işlemleri paralel olarak başlıyor...");

        await _cacheService.SetAsync(CacheKeys.OtpToken(notification.Otp, "twoFactorAuth"), notification.User.Id,
            TimeSpan.FromMinutes(5));

        var mailModel = new OtpMailViewModel
        {
            Title = "2 faktörlü kimlik doğrulama.",
            Description = "Giriş yapabilmek için, 6 haneli kodu doğrulayınız.",
            CustomerEmail = notification.Email,
            CustomerName = notification.FullName,
            ExpireInMinutes = 5,
            OtpCode = notification.Otp,
            RestaurantName = "Restauran Api"
        };

        await _mailFactory.SendAsync(MailTypes.Otp.ToString(), notification.Email, mailModel);
        
        _logger.LogInformation("✅ Mail arka plan job'a (Hangfire) başarıyla teslim edildi.");
    }
}