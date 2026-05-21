namespace RestaurantApi.Application.Models.MailModels;

public class OtpMailViewModel
{
    public string RestaurantName { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
    public int ExpireInMinutes { get; set; } = 5; 
    
    public string Title { get; set; } = "Güvenlik Doğrulaması";
    public string Description { get; set; } = null!;
}