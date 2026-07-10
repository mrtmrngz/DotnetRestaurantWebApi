using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Auth.Events;

public class ForgotPasswordVerifyEventHandler: INotificationHandler<ForgotPasswordVerifyEvent>
{
    private readonly ILogger<ForgotPasswordVerifyEventHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly IMailFactory _mailFactory;

    public ForgotPasswordVerifyEventHandler(ILogger<ForgotPasswordVerifyEventHandler> logger, ICacheService cacheService, IMailFactory mailFactory)
    {
        _logger = logger;
        _cacheService = cacheService;
        _mailFactory = mailFactory;
    }

    public async Task Handle(ForgotPasswordVerifyEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ForgotPasswordVerify event yakalandı! Mail ve Cache işlemleri paralel olarak başlıyor...");
        
        var validToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(notification.Token)
        );

        var cacheKey = CacheKeys.ForgotPasswordKey(validToken);

        await _cacheService.SetAsync(cacheKey, notification.UserId, TimeSpan.FromMinutes(notification.ExpiresInMinute));
        
        var url = $"http://testfrontend.com/forgot-password?token={validToken}";

        var mailModel = new ForgotPasswordViewModel
        {
            Title = "Parola değiştirme isteği.",
            RestaurantName = "Restaurant API",
            CustomerEmail = notification.Email,
            CustomerName = notification.FullName,
            Description = "Parolanızı değiştirmek için bağlantıya tıklayınız.",
            ExpireInMinutes = notification.ExpiresInMinute,
            ResetLink = url
        };

        await _mailFactory.SendAsync(MailTypes.ForgotPassword.ToString(), notification.Email, mailModel);
        
        _logger.LogInformation("✅ Mail arka plan job'a (Hangfire) başarıyla teslim edildi.");
    }
}