using Microsoft.Extensions.Logging;
using RestaurantApi.Application.Mail;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

namespace RestaurantApi.Infrastructure.Mail.BackgroundJobs;

public class MailHandlerManager: IMailHandlerManager
{
    private readonly IMailService _mailService;
    private readonly IMailTemplateRenderer _renderer;
    private readonly ILogger<MailHandlerManager> _logger;

    public MailHandlerManager(IMailService mailService, IMailTemplateRenderer renderer, ILogger<MailHandlerManager> logger)
    {
        _mailService = mailService;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task ExecuteWelcomeMailAsync(string to, WelcomeMailViewModel model)
    {
        _logger.LogInformation("Html basılıyor gönderiliryor...");
        var body = await _renderer.RenderAsync("Welcome.cshtml", model);
        var layout = new MailLayoutModel 
        {
            Title = "Hoş Geldiniz",
            RestaurantName = model.RestaurantName,
            Body = body,
            Year = DateTime.Now.Year
        };
        var html = await _renderer.RenderAsync("Layout.cshtml", layout);
        _logger.LogInformation("{To}: Mail gönderiliryor...", to);
        await _mailService.SendAsync(to, "Hoş Geldiniz", html);
    }
}