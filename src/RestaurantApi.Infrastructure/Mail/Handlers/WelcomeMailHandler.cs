using Hangfire;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Mail;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.BackgroundJobs;
using RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

namespace RestaurantApi.Infrastructure.Mail.Handlers;

public class WelcomeMailHandler: IMailHandler
{
    public string Type => MailTypes.Welcome.ToString();
    private readonly ILogger<WelcomeMailHandler> _logger;
    
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WelcomeMailHandler(IBackgroundJobClient backgroundJobClient, ILogger<WelcomeMailHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task SendAsync(string to, object model)
    {
        if (model is not WelcomeMailViewModel welcomeModel)
            throw new Exception("Model tipi WelcomeMailViewModel olmalı.");
        
        _logger.LogInformation("Mail gönderme, arka plan job oluşturuluyor. {To}", to);

        _backgroundJobClient.Enqueue<IMailHandlerManager>(x => x.ExecuteWelcomeMailAsync(to, welcomeModel));
    }
}