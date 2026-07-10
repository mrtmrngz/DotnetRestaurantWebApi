namespace RestaurantApi.Application.Models.MailModels;

public class ForgotPasswordViewModel
{
    public string RestaurantName { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    
    public string ResetLink { get; set; } = null!; 
    
    public int ExpireInMinutes { get; set; } = 15; 
    
    public string Title { get; set; } = "Şifre Sıfırlama Talebi";
    public string Description { get; set; } = "Hesabınızın şifresini sıfırlamak için bir talepte bulundunuz. Aşağıdaki butona tıklayarak yeni şifrenizi güvenli bir şekilde belirleyebilirsiniz.";
}