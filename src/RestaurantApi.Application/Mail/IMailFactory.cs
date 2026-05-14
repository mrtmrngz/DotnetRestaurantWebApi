namespace RestaurantApi.Application.Mail;

public interface IMailFactory
{
    Task SendAsync(string type, string to, object model);
}