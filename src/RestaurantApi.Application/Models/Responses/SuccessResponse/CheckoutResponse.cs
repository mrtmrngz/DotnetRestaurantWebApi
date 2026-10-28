using System.ComponentModel;

namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class CheckoutResponse: BaseResponse
{
    [DefaultValue("Online ödeme aracı için ödeme ekranı.")]
    public string SessionUrl { get; set; } = null!;
}