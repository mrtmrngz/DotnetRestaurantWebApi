using System.ComponentModel;

namespace RestaurantApi.Application.Models.Responses.SuccessResponse;

public class ProductCreateUpdateResponse: BaseResponse
{
    [DefaultValue("Ürün oluşturulma ve güncelleme sırasında dönecek response.")]
    public Guid Id { get; set; }
}