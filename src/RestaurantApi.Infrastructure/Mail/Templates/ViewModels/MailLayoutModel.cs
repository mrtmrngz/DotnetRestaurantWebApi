namespace RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

public class MailLayoutModel
{
    public string Title { get; set; } = null!;
    public string RestaurantName { get; set; } = null!;
    public string Body { get; set; } = null!;
    public int Year { get; set; } = DateTime.UtcNow.Year;
}