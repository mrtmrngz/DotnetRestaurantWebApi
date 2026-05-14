using RestaurantApi.Application.Mail;
using RestaurantApi.Domain.Enums;
using RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

namespace RestaurantApi.Infrastructure.Mail.Handlers;

public class WelcomeMailHandler: IMailHandler
{
    public string Type => MailTypes.Welcome.ToString();

    private readonly IMailService _mailService;
    private readonly IMailTemplateRenderer _renderer;

    public WelcomeMailHandler(IMailService mailService, IMailTemplateRenderer renderer)
    {
        _mailService = mailService;
        _renderer = renderer;
    }

    public async Task SendAsync(string to, object model)
    {
        if (model is not WelcomeMailViewModel welcomeModel)
            throw new Exception("Model tipi WelcomeMailViewModel olmalı.");

        var body = await _renderer.RenderAsync("Welcome.cshtml", welcomeModel);

        var layout = new MailLayoutModel
        {
            Title = "Hoş Geldiniz",
            RestaurantName = welcomeModel.RestaurantName,
            Body = body,
            Year = DateTime.Now.Year,
        };

        var html = await _renderer.RenderAsync("Layout.cshtml", layout);

        await _mailService.SendAsync(to, "Hoş Geldiniz", html);
    }
}