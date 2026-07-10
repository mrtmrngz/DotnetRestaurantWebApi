using Hangfire;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.BackgroundJobs;

namespace RestaurantApi.Infrastructure.Mail.Handlers;

public class ForgotPasswordMailHandler: IMailHandler
{
    public string Type => MailTypes.ForgotPassword.ToString();
    private readonly ILogger<ForgotPasswordMailHandler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ForgotPasswordMailHandler(ILogger<ForgotPasswordMailHandler> logger, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task SendAsync(string to, object model)
    {
        if (model is not ForgotPasswordViewModel forgotPasswordViewModel)
        {
            throw new Exception("Model tipi ForgotPasswordViewModel olmalı.");
        }
        
        _logger.LogInformation("Mail gönderme, arka plan job oluşturuluyor. {To}", to);
        
        _backgroundJobClient.Enqueue<IMailHandlerManager>(x => x.ExecuteForgotPasswordMailAsync(to, forgotPasswordViewModel));
    }
}