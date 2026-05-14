namespace RestaurantApi.Application.Mail;

public interface IMailService
{
    Task SendAsync(string to, string subject, string body);
}