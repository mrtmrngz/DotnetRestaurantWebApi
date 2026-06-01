using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;

namespace RestaurantApi.Application.Features.Auth.Events;

public class MailVerifiedEventHandler: INotificationHandler<MailVerifiedEvent>
{
    private readonly IMailFactory _mailFactory;
    private readonly ICacheService _cacheService;
    private readonly ILogger<MailVerifiedEventHandler> _logger;

    public MailVerifiedEventHandler(IMailFactory mailFactory, ICacheService cacheService, ILogger<MailVerifiedEventHandler> logger)
    {
        _mailFactory = mailFactory;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(MailVerifiedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("MailVerifiedEvent yakalandı, işlemler başlıyor.");

        var cacheKey = CacheKeys.MailVerificationToken(notification.Token);
        await _cacheService.RemoveAsync(cacheKey);
        
        _logger.LogInformation("Cache temizlendi: {token}", notification.Token);

        var mailModel = new WelcomeMailViewModel()
        {
            CustomerEmail = notification.User.Email!,
            CustomerName = $"{notification.User.Name} {notification.User.Surname}",
            RestaurantName = "Restaurant Api",
        };
        
        _logger.LogInformation("Mail gönderiliyor: {mail}", notification.User.Email);

        await _mailFactory.SendAsync(MailTypes.Welcome.ToString(), mailModel.CustomerEmail, mailModel);
    }
}