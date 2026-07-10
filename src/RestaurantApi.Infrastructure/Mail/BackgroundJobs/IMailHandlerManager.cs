
using RestaurantApi.Application.Models.MailModels;

namespace RestaurantApi.Infrastructure.Mail.BackgroundJobs;

public interface IMailHandlerManager
{
    Task ExecuteWelcomeMailAsync(string to, WelcomeMailViewModel model);
    Task ExecuteMailVerifyMailAsync(string to, VerifyMailViewModel model);
    Task ExecuteOtpMailAsync(string to, OtpMailViewModel model);
    Task ExecuteForgotPasswordMailAsync(string to, ForgotPasswordViewModel model);
}