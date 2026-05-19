using Hangfire;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.BackgroundJobs;

namespace RestaurantApi.Infrastructure.Mail.Handlers;

public class VerifyMailHandler: IMailHandler
{
    public string Type => MailTypes.Verify.ToString();
    private readonly ILogger<VerifyMailHandler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    
    public VerifyMailHandler(ILogger<VerifyMailHandler> logger, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task SendAsync(string to, object model)
    {
        if (model is not VerifyMailViewModel verifyModel)
            throw new Exception("Model tipi VerifyMailViewModel olmalı.");
        
        _logger.LogInformation("Mail gönderme, arka plan job oluşturuluyor. {To}", to);

        _backgroundJobClient.Enqueue<IMailHandlerManager>(x => x.ExecuteMailVerifyMailAsync(to, verifyModel));
    }
}