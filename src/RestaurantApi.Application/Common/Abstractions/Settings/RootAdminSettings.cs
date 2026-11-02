namespace RestaurantApi.Application.Common.Abstractions.Settings;

public class RootAdminSettings
{
    public const string SectionName = "RootAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}