namespace RestaurantApi.Application.Mail;

public interface IMailTemplateRenderer
{
    Task<string> RenderAsync<T>(string template, T model);
}