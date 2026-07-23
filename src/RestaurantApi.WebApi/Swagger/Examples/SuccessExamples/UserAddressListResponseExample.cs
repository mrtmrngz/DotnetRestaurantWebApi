using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Address.Queries.GetUserAddressQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class UserAddressListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>> GetExamples()
    {
        var addresses = new List<GetUserAddressQueryResult>
        {
            new GetUserAddressQueryResult(
                Id: Guid.Parse("019f81c8-fd18-7d7f-b21c-4f5a37e7fffc"),
                Title: "İş Yerim",
                RecipientName: "Mert Marangoz",
                City: "İstanbul",
                Town: "Şişli",
                Neighborhood: "Mecidiyeköy Mahallesi",
                Street: "Büyükdere Caddesi",
                BuildingNumber: "142A",
                BuildingInfo: null,
                PhoneNumber: "+905321234567",
                IsDefault: true,
                ZipCode: "34381"
            ),
            new GetUserAddressQueryResult(
                Id: Guid.Parse("019f81c9-7e7b-74e0-8ca2-a57b1cecec4c"),
                Title: "Evim",
                RecipientName: "Mert Marangoz",
                City: "Edirne",
                Town: "Merkez",
                Neighborhood: "Fatih Mahallesi",
                Street: "Aydınlar Caddesi",
                BuildingInfo: "Doğa Apartmanı B Blok D: 4",
                BuildingNumber: "8",
                PhoneNumber: "+905321234567",
                IsDefault: false,
                ZipCode: "22100"
            ),
            new GetUserAddressQueryResult(
                Id: Guid.Parse("019f81c8-1fc2-77b2-b691-2e6eb320461a"),
                Title: "Yazlık / Çiftlik",
                RecipientName: "Mert Marangoz",
                City: "Aydın",
                Town: "Söke",
                Neighborhood: "Yenidoğan Mahallesi",
                Street: "Atatürk Caddesi",
                BuildingInfo: null,
                BuildingNumber: "45",
                PhoneNumber: "+905321234567",
                IsDefault: false,
                ZipCode: "09200"
            )
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserAddressQueryResult>>(data: addresses);
    }
}