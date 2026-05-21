using Hangfire;
using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Mail;
using RestaurantApi.Application.Models.MailModels;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.BackgroundJobs;

namespace RestaurantApi.Infrastructure.Mail.Handlers;

public class OtpMailHandler: IMailHandler
{
    public string Type => MailTypes.Otp.ToString();
    private readonly ILogger<OtpMailHandler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    
    public OtpMailHandler(ILogger<OtpMailHandler> logger, IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }
    
    public async Task SendAsync(string to, object model)
    {
        if (model is not OtpMailViewModel otpMailViewModel)
            throw new Exception("Model tipi OtpMailViewModel olmalı.");
        
        _logger.LogInformation("Mail gönderme, arka plan job oluşturuluyor. {To}", to);

        _backgroundJobClient.Enqueue<IMailHandlerManager>(x => x.ExecuteOtpMailAsync(to, otpMailViewModel));
    }
}