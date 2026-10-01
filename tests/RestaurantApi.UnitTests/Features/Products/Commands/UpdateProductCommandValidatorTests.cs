using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using RestaurantApi.Application.Features.Products.Commands.UpdateProductCommand;

namespace RestaurantApi.UnitTests.Features.Products.Commands;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator;

    public UpdateProductCommandValidatorTests()
    {
        _validator = new UpdateProductCommandValidator();
    }

    private const string AtLeastOneFieldMessage = "Güncellenecek en az bir alan belirtilmelidir.";

    #region Helper Methods

    private static UpdateProductCommand CreateValidCommand()
    {
        return new UpdateProductCommand(
            Title: "Geçerli Ürün Adı",
            Description: "Geçerli ürün açıklaması",
            Price: 150.50m,
            CategoryId: Guid.NewGuid(),
            Images: CreateFormFileCollection(count: 1),
            DeletedImagePublicIds: new List<string> { "public-id-1" }
        );
    }

    private static IFormFileCollection CreateFormFileCollection(
        int count = 1,
        string fileName = "image.jpg",
        string contentType = "image/jpeg",
        long sizeInBytes = 1024 * 1024)
    {
        var collection = new FormFileCollection();

        for (int i = 0; i < count; i++)
        {
            collection.Add(CreateMockFormFile(
                fileName: count > 1 ? $"image_{i}{Path.GetExtension(fileName)}" : fileName,
                contentType: contentType,
                lengthInBytes: sizeInBytes));
        }

        return collection;
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, long lengthInBytes)
    {
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns(fileName);
        mockFile.ContentType.Returns(contentType);
        mockFile.Length.Returns(lengthInBytes);
        return mockFile;
    }

    #endregion

    #region At Least One Field Rule Tests

    [Fact]
    public void Validate_WhenAllFieldsAreNull_ShouldHaveAtLeastOneFieldError()
    {
        var command = new UpdateProductCommand();

        var result = _validator.TestValidate(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == AtLeastOneFieldMessage);
    }

    [Fact]
    public void Validate_WhenOnlyEmptyDeletedImagePublicIdsListProvided_ShouldHaveAtLeastOneFieldError()
    {
        var command = new UpdateProductCommand(DeletedImagePublicIds: new List<string>());

        var result = _validator.TestValidate(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == AtLeastOneFieldMessage);
    }

    [Fact]
    public void Validate_WhenOnlyTitleProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(Title: "Geçerli Başlık");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyDescriptionProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(Description: "Geçerli Açıklama");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyPriceProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(Price: 100m);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyCategoryIdProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(CategoryId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyImagesProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(Images: CreateFormFileCollection(count: 1));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyDeletedImagePublicIdsProvided_ShouldNotHaveValidationError()
    {
        var command = new UpdateProductCommand(DeletedImagePublicIds: new List<string> { "public-id-1" });

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Title Validations

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Validate_WhenTitleIsShorterThanTwoCharacters_ShouldHaveValidationError(string invalidTitle)
    {
        var command = CreateValidCommand() with { Title = invalidTitle };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Ürün adı en az 2 karakter olmalıdır.");
    }

    [Fact]
    public void Validate_WhenTitleExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { Title = new string('A', 151) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Ürün adı en fazla 150 karakter olabilir.");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(150)]
    public void Validate_WhenTitleIsAtLengthBoundaries_ShouldNotHaveValidationError(int length)
    {
        var command = CreateValidCommand() with { Title = new string('A', length) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenTitleIsNull_ShouldNotRunTitleRules()
    {
        var command = CreateValidCommand() with { Title = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Description Validations

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { Description = new string('A', 501) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Ürün açıklaması en fazla 500 karakter olabilir.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsAtMaxLength_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { Description = new string('A', 500) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldNotRunDescriptionRules()
    {
        var command = CreateValidCommand() with { Description = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Price Validations

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(-0.01d)]
    public void Validate_WhenPriceIsZeroOrNegative_ShouldHaveGreaterThanZeroError(double invalidPrice)
    {
        var command = CreateValidCommand() with { Price = (decimal)invalidPrice };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Ürün fiyatı 0'dan büyük olmalıdır.");
    }

    [Theory]
    [InlineData(10000.01d)]
    [InlineData(10001d)]
    public void Validate_WhenPriceExceedsMaximum_ShouldHaveLessThanOrEqualError(double invalidPrice)
    {
        var command = CreateValidCommand() with { Price = (decimal)invalidPrice };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Ürün fiyatı 10.000'den küçük veya eşit olmalıdır.");
    }

    [Theory]
    [InlineData(0.01d)]
    [InlineData(100d)]
    [InlineData(9999.99d)]
    [InlineData(10000d)]
    public void Validate_WhenPriceIsValid_ShouldNotHaveValidationError(double validPrice)
    {
        var command = CreateValidCommand() with { Price = (decimal)validPrice };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WhenPriceIsNull_ShouldNotRunPriceRules()
    {
        var command = CreateValidCommand() with { Price = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    #endregion

    #region CategoryId Validations

    [Fact]
    public void Validate_WhenCategoryIdIsEmptyGuid_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
              .WithErrorMessage("Ürün kategori id boş olamaz.");
    }

    [Fact]
    public void Validate_WhenCategoryIdIsValid_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_WhenCategoryIdIsNull_ShouldNotRunCategoryRules()
    {
        var command = CreateValidCommand() with { CategoryId = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    #endregion

    #region Images Collection Validations

    [Fact]
    public void Validate_WhenImagesCollectionIsEmpty_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { Images = new FormFileCollection() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Images)
              .WithErrorMessage("Yüklenen görsel listesi boş olamaz.");
    }

    [Fact]
    public void Validate_WhenImagesIsNull_ShouldNotRunImageCollectionRules()
    {
        var command = CreateValidCommand() with { Images = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Images);
    }

    [Fact]
    public void Validate_WhenImagesCountIsAtMaximum_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { Images = CreateFormFileCollection(count: 10) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Images);
    }

    [Fact]
    public void Validate_WhenImagesCountExceedsMaximum_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { Images = CreateFormFileCollection(count: 11) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Images)
              .WithErrorMessage("En fazla 10 ürün görseli yüklenebilir.");
    }

    #endregion

    #region Individual Image Validations (RuleForEach)

    [Theory]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.txt", "text/plain")]
    [InlineData("image.bmp", "image/bmp")]
    public void Validate_WhenImageHasUnsupportedExtension_ShouldHaveValidationError(string fileName, string contentType)
    {
        var command = CreateValidCommand() with
        {
            Images = CreateFormFileCollection(count: 1, fileName: fileName, contentType: contentType)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.");
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.webp", "image/webp")]
    [InlineData("IMAGE.PNG", "image/png")]
    public void Validate_WhenImageHasSupportedExtensionAndContentType_ShouldNotHaveValidationError(
        string fileName, string contentType)
    {
        var command = CreateValidCommand() with
        {
            Images = CreateFormFileCollection(count: 1, fileName: fileName, contentType: contentType)
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Images[0]");
    }

    [Fact]
    public void Validate_WhenImageContentTypeIsInvalid_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Images = CreateFormFileCollection(count: 1, fileName: "image.jpg", contentType: "application/pdf")
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Geçersiz görsel içerik türü.");
    }

    [Fact]
    public void Validate_WhenImageSizeExceedsMaximum_ShouldHaveValidationError()
    {
        var maxFileSize = 5 * 1024 * 1024;
        var command = CreateValidCommand() with
        {
            Images = CreateFormFileCollection(count: 1, sizeInBytes: maxFileSize + 1)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Her ürün görselinin boyutu 0'dan büyük ve en fazla 5 MB olabilir.");
    }

    [Fact]
    public void Validate_WhenImageSizeIsZero_ShouldHaveInvalidFileError()
    {
        // BeValidFile kuralı (Length > 0) boyut kuralından önce çalıştığı için önce bu mesaj döner.
        var command = CreateValidCommand() with
        {
            Images = CreateFormFileCollection(count: 1, sizeInBytes: 0)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.");
    }

    [Fact]
    public void Validate_WhenImageFileNameIsWhiteSpace_ShouldHaveInvalidFileError()
    {
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("   ");
        mockFile.ContentType.Returns("image/jpeg");
        mockFile.Length.Returns(1024);

        var command = CreateValidCommand() with { Images = new FormFileCollection { mockFile } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.");
    }

    [Fact]
    public void Validate_WhenImageContentTypeIsEmpty_ShouldHaveInvalidFileError()
    {
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("image.jpg");
        mockFile.ContentType.Returns("");
        mockFile.Length.Returns(1024);

        var command = CreateValidCommand() with { Images = new FormFileCollection { mockFile } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[0]")
              .WithErrorMessage("Geçersiz dosya. Dosya boş veya bozuk olabilir.");
    }

    [Fact]
    public void Validate_WhenSecondImageIsInvalid_ShouldHaveValidationErrorForThatImage()
    {
        var collection = new FormFileCollection
        {
            CreateMockFormFile("image_0.jpg", "image/jpeg", 1024),
            CreateMockFormFile("document.txt", "text/plain", 1024)
        };

        var command = CreateValidCommand() with { Images = collection };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Images[1]")
              .WithErrorMessage("Desteklenen görsel formatları: JPG, JPEG, PNG ve WEBP.");
    }

    #endregion

    #region DeletedImagePublicIds Validations

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDeletedImagePublicIdIsEmptyOrWhiteSpace_ShouldHaveValidationError(string invalidId)
    {
        var command = CreateValidCommand() with { DeletedImagePublicIds = new List<string> { invalidId } };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DeletedImagePublicIds[0]")
              .WithErrorMessage("Silinecek görsel public id'si boş olamaz.");
    }

    [Fact]
    public void Validate_WhenOneOfDeletedImagePublicIdsIsEmpty_ShouldHaveValidationErrorForThatIndex()
    {
        var command = CreateValidCommand() with
        {
            DeletedImagePublicIds = new List<string> { "public-id-1", "" }
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DeletedImagePublicIds[1]")
              .WithErrorMessage("Silinecek görsel public id'si boş olamaz.");
    }

    [Fact]
    public void Validate_WhenDeletedImagePublicIdsIsNull_ShouldNotRunRules()
    {
        var command = CreateValidCommand() with { DeletedImagePublicIds = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DeletedImagePublicIds);
    }

    #endregion

    #region Happy Path

    [Fact]
    public void Validate_WhenAllFieldsAreValid_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}