using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Address.Commands.UpdateAddressCommand;

namespace RestaurantApi.UnitTests.Features.Address.Commands;

public class UpdateAddressCommandValidatorTest
{
    private readonly UpdateAddressCommandValidator _validator;

    public UpdateAddressCommandValidatorTest()
    {
        _validator = new UpdateAddressCommandValidator();
    }
    
    #region At Least One Field Rule Tests

    [Fact]
    public void Validator_WhenAllFieldsAreNull_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateBaseCommand(); // Tamamen null alanlardan oluşan komut

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("UpdateAddress")
              .WithErrorMessage("Güncelleme yapmak için en az bir alan belirtmelisiniz.");
    }

    [Fact]
    public void Validator_WhenAtLeastOneFieldProvided_ShouldNotHaveValidationErrorForUpdateAddress()
    {
        // Arrange
        var command = CreateBaseCommand() with { Title = "Ev Adresim" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("UpdateAddress");
    }

    #endregion

    #region Empty String Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validator_WhenFieldsAreEmptyString_ShouldHaveValidationError(string emptyValue)
    {
        // Arrange
        var command = CreateBaseCommand() with
        {
            Title = emptyValue,
            RecipientName = emptyValue,
            City = emptyValue,
            Town = emptyValue,
            Neighborhood = emptyValue,
            Street = emptyValue,
            BuildingNumber = emptyValue,
            PhoneNumber = emptyValue
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Adres başlığı girmek zorundasınız.");
        result.ShouldHaveValidationErrorFor(x => x.RecipientName).WithErrorMessage("Alıcı kişi adı boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.City).WithErrorMessage("Şehir boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.Town).WithErrorMessage("İlçe adı boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.Neighborhood).WithErrorMessage("Mahalle adı boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.Street).WithErrorMessage("Sokak adı boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.BuildingNumber).WithErrorMessage("Bina numarası boş bırakılamaz.");
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber).WithErrorMessage("Telefon numarası boş bırakılamaz.");
    }

    #endregion

    #region Maximum Length Validation Tests

    [Fact]
    public void Validator_WhenFieldsExceedMaximumLength_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateBaseCommand() with
        {
            Title = new string('A', 51),
            RecipientName = new string('A', 101),
            City = new string('A', 51),
            Town = new string('A', 101),
            Neighborhood = new string('A', 71),
            Street = new string('A', 86),
            BuildingInfo = new string('A', 151),
            BuildingNumber = new string('A', 16),
            PhoneNumber = new string('1', 16),
            ZipCode = new string('1', 11)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Adres başlığı en fazla 50 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.RecipientName).WithErrorMessage("Alıcı kişi adı en fazla 100 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.City).WithErrorMessage("Şehir en fazla 50 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.Town).WithErrorMessage("İlçe adı en fazla 100 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.Neighborhood).WithErrorMessage("Mahalle adı en fazla 70 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.Street).WithErrorMessage("Sokak adı en fazla 85 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.BuildingInfo).WithErrorMessage("Bina bilgisi en fazla 150 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.BuildingNumber).WithErrorMessage("Bina numarası en fazla 15 karakter olabilir.");
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber).WithErrorMessage("Telefon numarası maksimum 15 karakter olmalıdır.");
        result.ShouldHaveValidationErrorFor(x => x.ZipCode).WithErrorMessage("Posta kodu en fazla 10 karakter olabilir.");
    }

    #endregion

    #region Happy Path Test

    [Fact]
    public void Validator_WhenAllFieldsAreValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new UpdateAddressCommand(
            Title: "İş Yeri",
            RecipientName: "Mert Marangoz",
            City: "Aydın",
            Town: "Söke",
            Neighborhood: "Atatürk Mah.",
            Street: "Cumhuriyet Cad.",
            BuildingInfo: "Plaza A Blok",
            BuildingNumber: "12A",
            PhoneNumber: "+905441234567", // BeValidTurkishNumber'dan geçecek geçerli bir no
            IsDefault: true,
            ZipCode: "09200"
        ) { AddressId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Helper Methods

    private UpdateAddressCommand CreateBaseCommand()
    {
        return new UpdateAddressCommand(
            Title: null,
            RecipientName: null,
            City: null,
            Town: null,
            Neighborhood: null,
            Street: null,
            BuildingInfo: null,
            BuildingNumber: null,
            PhoneNumber: null,
            IsDefault: null,
            ZipCode: null
        )
        {
            AddressId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
    }

    #endregion
}