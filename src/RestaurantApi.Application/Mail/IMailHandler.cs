namespace RestaurantApi.Application.Mail;

public interface IMailHandler
{
    string Type { get; }
    Task SendAsync(string to, object model);
}