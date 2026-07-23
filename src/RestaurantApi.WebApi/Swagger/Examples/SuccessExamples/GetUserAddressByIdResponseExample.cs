using RestaurantApi.Application.Features.Address.Queries.GetUserAddressByIdQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserAddressByIdResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>>
{
    public GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult> GetExamples()
    {
        var address = new GetUserAddressByIdQueryResult(
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
        );
        
        return new GeneralSuccessResponseWithData<GetUserAddressByIdQueryResult>(data: address);
    }
}