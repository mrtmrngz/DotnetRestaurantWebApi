using System.ComponentModel;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Models.Responses.ErrorResponses;

public class ValidationErrorResponse
{
    [DefaultValue(400)] 
    public int StatusCode { get; set; }

    [DefaultValue("Validasyon Hatası.")]
    public string Message { get; set; } = "Validasyon Hatası.";

    [DefaultValue(Codes.VALIDATION_ERROR)]
    public Codes Code { get; set; } = Codes.VALIDATION_ERROR;

    public List<ErrorList> Errors { get; set; } = new();
}

public class ErrorList
{
    [DefaultValue("Hata başlığı.")]
    public required string Field { get; set; }

    [DefaultValue("Hata mesajı.")]
    public required string Message { get; set; }
}