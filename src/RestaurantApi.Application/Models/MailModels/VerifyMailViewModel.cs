namespace RestaurantApi.Application.Models.MailModels;

public class VerifyMailViewModel
{
    public string RestaurantName { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string VerifyUrl { get; set; } = null!;
}