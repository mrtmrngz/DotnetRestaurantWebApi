using RestaurantApi.Application.Mail;

namespace RestaurantApi.Infrastructure.Mail.Factory;

public class MailFactory: IMailFactory
{
    private readonly IEnumerable<IMailHandler> _handlers;

    public MailFactory(IEnumerable<IMailHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task SendAsync(string type, string to, object model)
    {
        var handlers = _handlers.FirstOrDefault(x => x.Type == type);

        if (handlers == null)
            throw new Exception("Geçersiz mail tipi.");

        await handlers.SendAsync(to, model);
    }
}