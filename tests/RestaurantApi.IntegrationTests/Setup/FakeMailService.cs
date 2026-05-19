using RestaurantApi.Application.Mail;

namespace RestaurantApi.IntegrationTests.Setup;

public class FakeMailService: IMailService
{
    public Task SendAsync(string to, string subject, string body)
    {
        return Task.CompletedTask;
    }
}