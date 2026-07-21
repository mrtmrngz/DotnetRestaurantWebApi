using RestaurantApi.Application.Features.Address.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AddressExamples;

public class CreateAddressExample : IExamplesProvider<CreateAddressCommand>
{
    public CreateAddressCommand GetExamples()
    {
        return new CreateAddressCommand(
            Title: "Ev adresim",
            RecipientName: "John Doe",
            City: "İstanbul",
            Town: "Şişli",
            Neighborhood: ".NET Mahallesi",
            Street: "Swagger Sokak",
            BuildingInfo: "LINQ Karşısındaki Mavi Apt",
            BuildingNumber: "15A",
            PhoneNumber: "+905421234567",
            IsDefault: true,
            ZipCode: "34200"
        );
    }
}