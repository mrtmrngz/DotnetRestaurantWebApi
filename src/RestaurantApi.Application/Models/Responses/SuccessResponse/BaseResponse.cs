using System.ComponentModel;
using RestaurantApi.Application.Common.Enums;

namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class BaseResponse
{
    [DefaultValue("Başarılı yanıt mesajı.")]
    public string? Message { get; set; }
    [DefaultValue("FETCH_DATA_SUCCESS")]
    public Codes Code { get; set; }
}