using RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

namespace RestaurantApi.Infrastructure.Mail.BackgroundJobs;

public interface IMailHandlerManager
{
    Task ExecuteWelcomeMailAsync(string to, WelcomeMailViewModel model);
}