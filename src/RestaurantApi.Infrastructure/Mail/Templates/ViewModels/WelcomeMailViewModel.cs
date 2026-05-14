namespace RestaurantApi.Infrastructure.Mail.Templates.ViewModels;

public class WelcomeMailViewModel
{
    public string RestaurantName { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string? CustomerPhone { get; set; }
    public string? AppUrl { get; set; }
}