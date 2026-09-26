using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using RestaurantApi.Application.Features.Products.Commands.CreateProductCommand;

namespace RestaurantApi.UnitTests.Features.Products.Commands;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _validator = new CreateProductCommandValidator();
    }

    #region Helper Methods

    private static CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand(
            Title: "Geçerli Ürün Adı",
            Description: "Geçerli ürün açıklaması",
            Price: 150.50m,
            CategoryId: Guid.NewGuid(),
            Images: CreateFormFileCollection(count: 1)
        );
    }

    private static IFormFileCollection CreateFormFileCollection(int count = 1, string fileName = "image.jpg", string contentType = "image/jpeg", long sizeInBytes = 1024 * 1024)
    {
        var collection = new FormFileCollection();
        
        for (int i = 0; i < count; i++)
        {
            collection.Add(CreateMockFormFile(
                fileName: count > 1 ? $"image_{i}{Path.GetExtension(fileName)}" : fileName,
                contentType: contentType,
                lengthInBytes: sizeInBytes
            ));
        }

        return collection;
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, long lengthInBytes)
    {
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns(fileName);
        mockFile.ContentType.Returns(contentType);
        mockFile.Length.Returns(lengthInBytes);
        mockFile.OpenReadStream().Returns(new MemoryStream(new byte[lengthInBytes]));
        return mockFile;
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task ValidateAsync_ShouldBeValid_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Title Validations

    [Theory]
    [InlineData(null, "Ürün adı zorunludur.")]
    [InlineData("", "Ürün adı boş olamaz.")]
    [InlineData(" ", "Ürün adı boş olamaz.")]
    [InlineData("A", "Ürün adı en az 2 karakter olmalıdır.")]
    public async Task ValidateAsync_ShouldHaveError_WhenTitleIsInvalid(string title, string expectedErrorMessage)
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Title = title }; // Record 'with' ifadesi

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Title = new string('A', 151) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Ürün adı en fazla 150 karakter olabilir.");
    }

    #endregion

    #region Description Validations

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Description = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Ürün açıklaması en fazla 500 karakter olabilir.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldBeValid_WhenDescriptionIsNull()
    {
        // Arrange
        var command = CreateValidCommand();
        var validCommand = command with { Description = null };

        // Act
        var result = await _validator.TestValidateAsync(validCommand);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Price Validations

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task ValidateAsync_ShouldHaveError_WhenPriceIsLessThanOrEqualToZero(decimal price)
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Price = price };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Ürün fiyatı 0'dan büyük olmalıdır.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenPriceExceedsMaximumLimit()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Price = 10000.01m };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Ürün fiyatı 10.000'den küçük olmalıdır"); // Not: Validator kodunuzdaki tam metin
    }

    [Fact]
    public async Task ValidateAsync_ShouldBeValid_WhenPriceIsExactlyAtMaximumLimit()
    {
        // Arrange
        var command = CreateValidCommand();
        var validCommand = command with { Price = 10000m };

        // Act
        var result = await _validator.ValidateAsync(validCommand);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region CategoryId Validations

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenCategoryIdIsEmpty()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { CategoryId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
              .WithErrorMessage("Ürün kategori id boş olamaz.");
    }

    #endregion

    #region Images Collection Validations

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImagesIsNull()
    {
        // Arrange
        var command = CreateValidCommand();
        // Null atanırken compiler uyarısını (CS8625) bilinçli olarak bastırıyoruz çünkü validator'ün null kontrolünü test ediyoruz.
        var invalidCommand = command with { Images = null! }; 

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Images)
              .WithErrorMessage("En az 1 ürün görseli yüklenmelidir.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImagesCollectionIsEmpty()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = new FormFileCollection() };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Images)
              .WithErrorMessage("En az 1 ürün görseli yüklenmelidir.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImageCountExceedsMaximum()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = CreateFormFileCollection(count: 11) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Images)
              .WithErrorMessage("En fazla 10 ürün görseli yüklenebilir.");
    }

    #endregion

    #region Individual Image Validations (RuleForEach)

    [Theory]
    [InlineData(".gif", "image/gif", 1024, "Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.")]
    [InlineData(".txt", "text/plain", 1024, "Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.")]
    [InlineData(".jpg", "application/pdf", 1024, "Geçersiz görsel içerik türü.")]
    public async Task ValidateAsync_ShouldHaveError_WhenImageHasInvalidExtensionOrContentType(
        string fileName, string contentType, long length, string expectedErrorMessage)
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = CreateFormFileCollection(count: 1, fileName: fileName, contentType: contentType, sizeInBytes: length) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage(expectedErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImageSizeExceedsMaximum()
    {
        // Arrange (5MB + 1 byte)
        var maxFileSize = 5 * 1024 * 1024;
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = CreateFormFileCollection(count: 1, sizeInBytes: maxFileSize + 1) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Her ürün görselinin boyutu 0'dan büyük ve en fazla 5 MB olabilir.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImageSizeIsZero()
    {
        // Arrange
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = CreateFormFileCollection(count: 1, sizeInBytes: 0) };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Her ürün görselinin boyutu 0'dan büyük ve en fazla 5 MB olabilir.");
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveError_WhenImageFileNameOrContentTypeIsWhiteSpace()
    {
        // Arrange
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("   ");
        mockFile.ContentType.Returns("image/jpeg");
        mockFile.Length.Returns(1024);
        var collection = new FormFileCollection { mockFile };
        var command = CreateValidCommand();
        var invalidCommand = command with { Images = collection };

        // Act
        var result = await _validator.TestValidateAsync(invalidCommand);

        // Assert
        result.ShouldHaveValidationErrorFor("Images[0]")
            .WithErrorMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.");
    }

    #endregion    
}