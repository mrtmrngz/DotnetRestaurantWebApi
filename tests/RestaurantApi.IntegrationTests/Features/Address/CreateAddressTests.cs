using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Address.Commands;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;

namespace RestaurantApi.IntegrationTests.Features.Address;

public class CreateAddressTests: BaseIntegrationTest
{
    public CreateAddressTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    
    // SUCCESS TEST START

    [Fact]
    public async Task CreateAddress_WhenValidData_ShouldReturnOK()
    {
        var setupResult = await CreateVanillaUserAsync();
        var command = new CreateAddressCommand(
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

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync("/api/addresses", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadContentAsAsync<BaseResponse>();

        result.Should().NotBeNull("Resul boş gelmemeli.");
        result.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS, "Result içerisinde code CONTENT_CREATED_SUCCESS olmalıydı");
        result.Message.Should().Be("Adres başarılı bir şekilde oluşturuldu.");
    }
    
    
    // SUCCESS TEST END
    
    // ERROR TESTS START

    [Fact]
    public async Task CreateAddress_WhenValidationErrorAcquired_ShouldReturnBadRequest()
    {
        var setupResult = await CreateVanillaUserAsync();
        var command = new CreateAddressCommand(
            Title: "",
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

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync("/api/addresses", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.ReadContentAsAsync<ValidationErrorResponse>();

        result.Should().NotBeNull("Resul boş gelmemeli.");
        result.Code.Should().Be(Codes.VALIDATION_ERROR, "Result içerisindeki code VALIDATION_ERROR olmalıydı.");
    }
    
    [Fact]
    public async Task CreateAddress_WhenUserUnauthorized_ShouldReturn401()
    {
        var command = new CreateAddressCommand(
            Title: "Ev Adresim",
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

        var response = await Client.PostAsJsonAsync("/api/addresses", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı yetkili olmadığı için 401 kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
}