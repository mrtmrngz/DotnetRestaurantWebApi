using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Address.Commands;

namespace RestaurantApi.UnitTests.Features.Address.Commands;

public class AddressCommandValidatorTests
{
    private readonly CreateAddressCommandValidator _validator;

    public AddressCommandValidatorTests()
    {
        _validator = new CreateAddressCommandValidator();
    }

    // SUCCESS TEST START

    [Theory]
    [InlineData(
        "Ev Adresim", "John Doe", "İstanbul", "Kadıköy", "Moda Mahallesi",
        "Bahariye Caddesi", "Mavi Apartmanı No: 4", "12A", "+905421234567", true, "34710")]
    [InlineData(
        "İş Adresi", "Ahmet Yılmaz", "Ankara", "Çankaya", "Kızılay Mah.",
        "Atatürk Bulvarı", null, "100", "05321234567", false, null)]
    [InlineData(
        "Yazlık", "Mehmet Demir", "İzmir", "Çeşme", "Alaçatı Mah.",
        "1001. Sokak", "", "5", "5051234567", false, "")]
    [InlineData(
        "A", "A", "A", "A", "A",
        "A", "A", "1", "+905555555555", true, "12345")]
    public async Task CreateAddressCommandValidator_WhenValidData_ShouldNotHaveValidationError(
        string title,
        string recipientName,
        string city,
        string town,
        string neighborhood,
        string street,
        string? buildingInfo,
        string buildingNumber,
        string phoneNumber,
        bool isDefault,
        string? zipCode
    )
    {
        var command = new CreateAddressCommand(title, recipientName, city, town, neighborhood, street, buildingInfo,
            buildingNumber, phoneNumber, isDefault, zipCode);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // SUCCESS TEST END

    // ERROR TEST START

    [Theory]
    [InlineData("", "John", "İst", "Şişli", "Mah", "Sok", "Bina", "15", "+905421234567", "Title")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "John", "İst", "Şişli", "Mah", "Sok", "Bina",
        "15", "+905421234567", "Title")]
    [InlineData("Ev", "", "İst", "Şişli", "Mah", "Sok", "Bina", "15", "+905421234567",
        "RecipientName")]
    [InlineData("Ev", "John", "İst", "Şişli", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
        "Sok", "Bina", "15", "+905421234567", "Neighborhood")]
    [InlineData("Ev", "John", "İst", "Şişli", "Mah",
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Bina", "15",
        "+905421234567", "Street")]
    [InlineData("Ev", "John", "İst", "Şişli", "Mah", "Sok", "Bina", "15", "", "PhoneNumber")]
    [InlineData("Ev", "John", "İst", "Şişli", "Mah", "Sok", "Bina", "15", "12345",
        "PhoneNumber")]
    [InlineData("Ev", "John", "İst", "Şişli", "Mah", "Sok", "Bina", "AAAAAAAAAAAAAAAA", "+905421234567",
        "BuildingNumber")]
    public async Task CreateAddressCommandValidator_WhenInvalidData_ShouldHaveValidationError(
        string title,
        string recipientName,
        string city,
        string town,
        string neighborhood,
        string street,
        string? buildingInfo,
        string buildingNumber,
        string phoneNumber,
        string expectedErrorProperty)
    {
        var validator = new CreateAddressCommandValidator();
        var command = new CreateAddressCommand(
            Title: title,
            RecipientName: recipientName,
            City: city,
            Town: town,
            Neighborhood: neighborhood,
            Street: street,
            BuildingInfo: buildingInfo,
            BuildingNumber: buildingNumber,
            PhoneNumber: phoneNumber,
            IsDefault: true,
            ZipCode: "34200"
        );

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(expectedErrorProperty);
    }

    // ERROR TEST END
}