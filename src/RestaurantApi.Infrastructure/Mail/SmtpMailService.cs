using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using RestaurantApi.Application.Mail;
using RestaurantApi.Infrastructure.Settings;

namespace RestaurantApi.Infrastructure.Mail;

public class SmtpMailService: IMailService
{
    private readonly MailSettings _settings;

    public SmtpMailService(IOptions<MailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var email = new MimeMessage();
        
        email.From.Add(MailboxAddress.Parse(_settings.Email));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        email.Body = new TextPart("html")
        {
            Text = body
        };

        using var smpt = new SmtpClient();

        await smpt.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await smpt.AuthenticateAsync(_settings.Email, _settings.Password);
        await smpt.SendAsync(email);
        await smpt.DisconnectAsync(true);
    }
}