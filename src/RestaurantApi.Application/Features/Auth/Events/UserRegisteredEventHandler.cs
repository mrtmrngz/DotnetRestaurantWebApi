using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Auth.Events;

public class UserRegisteredEventHandler: INotificationHandler<UserRegisteredEvent>
{
    private readonly ICacheService _cacheService;
    private readonly IMailFactory _mailFactory;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(ICacheService cacheService, IMailFactory mailFactory, ILogger<UserRegisteredEventHandler> logger)
    {
        _cacheService = cacheService;
        _mailFactory = mailFactory;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📢 UserRegisteredEvent yakalandı! Mail ve Cache işlemleri paralel olarak başlıyor...");
        
        var validToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Text.Encoding.UTF8.GetBytes(notification.Token)
        );
        
        string confirmationToken = $"http://testfrontend.com/verify-email?token={validToken}";
        
        await _cacheService.SetAsync(CacheKeys.MailVerificationToken(validToken), notification.User.Id, TimeSpan.FromMinutes(5));
        
        var mailModel = new VerifyMailViewModel
        {
            CustomerEmail = notification.Email,
            CustomerName = notification.FullName,
            RestaurantName = "Restaurant Api",
            VerifyUrl = confirmationToken
        };

        await _mailFactory.SendAsync(MailTypes.Verify.ToString(), notification.Email, mailModel);
        
        _logger.LogInformation("✅ Mail arka plan job'a (Hangfire) başarıyla teslim edildi.");
    }
}