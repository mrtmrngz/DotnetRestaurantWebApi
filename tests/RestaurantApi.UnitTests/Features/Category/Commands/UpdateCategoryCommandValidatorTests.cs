using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using RestaurantApi.Application.Features.Category.Commands.UpdateCategoryCommand;

namespace RestaurantApi.UnitTests.Features.Category.Commands;

public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator;

    public UpdateCategoryCommandValidatorTests()
    {
        _validator = new UpdateCategoryCommandValidator();
    }

    #region At Least One Field Rule Tests

    [Fact]
    public void Validate_WhenAllFieldsAreNull_ShouldHaveValidationErrorForObject()
    {
        var command = new UpdateCategoryCommand(Title: null, Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("UpdateCategory")
              .WithErrorMessage("Güncelleme yapmak için en az bir alan belirtmelisiniz.");
    }

    [Fact]
    public void Validate_WhenOnlyTitleIsProvided_ShouldNotHaveAtLeastOneFieldError()
    {
        var command = new UpdateCategoryCommand(Title: "Yeni Kategori", Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("UpdateCategory");
    }

    [Fact]
    public void Validate_WhenOnlyImageIsProvided_ShouldNotHaveAtLeastOneFieldError()
    {
        var fileMock = CreateMockFile("valid.png", 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("UpdateCategory");
    }

    #endregion

    #region Title Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTitleIsEmptyOrWhitespace_ShouldHaveValidationError(string invalidTitle)
    {
        var command = new UpdateCategoryCommand(Title: invalidTitle, Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Kategori adı boş bırakılamaz.");
    }

    [Fact]
    public void Validate_WhenTitleIsLessThan2Characters_ShouldHaveValidationError()
    {
        var command = new UpdateCategoryCommand(Title: "A", Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Kategori adı en az 2 karakter olmalıdır.");
    }

    [Fact]
    public void Validate_WhenTitleExceeds100Characters_ShouldHaveValidationError()
    {
        var invalidTitle = new string('A', 101);
        var command = new UpdateCategoryCommand(Title: invalidTitle, Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Kategori adı en fazla 100 karakter olabilir.");
    }

    [Fact]
    public void Validate_WhenTitleIsNull_ShouldNotRunTitleValidationRules()
    {
        var fileMock = CreateMockFile("valid.png", 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Image Extension Tests

    [Theory]
    [InlineData("test.jpg")]
    [InlineData("test.jpeg")]
    [InlineData("test.png")]
    [InlineData("test.webp")]
    [InlineData("TEST.PNG")]
    public void Validate_WhenImageHasValidExtension_ShouldNotHaveExtensionError(string fileName)
    {
        var fileMock = CreateMockFile(fileName, 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var commandToTest = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(commandToTest);

        result.ShouldNotHaveValidationErrorFor(x => x.Image);
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("script.exe")]
    [InlineData("image.gif")]
    [InlineData("noextension")]
    public void Validate_WhenImageHasInvalidExtension_ShouldHaveValidationError(string fileName)
    {
        var fileMock = CreateMockFile(fileName, 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Image)
              .WithErrorMessage("Sadece .jpg, .jpeg, .png veya .webp formatında görseller yüklenebilir.");
    }

    #endregion

    #region Image Size Tests

    [Fact]
    public void Validate_WhenImageExceeds5MB_ShouldHaveValidationError()
    {
        var fileMock = CreateMockFile("large.png", 5 * 1024 * 1024 + 1);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Image)
              .WithErrorMessage("Görsel boyutu en fazla 5MB olabilir.");
    }

    [Fact]
    public void Validate_WhenImageIsExactly5MB_ShouldNotHaveSizeError()
    {
        var fileMock = CreateMockFile("valid.png", 5 * 1024 * 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Image);
    }

    #endregion

    #region Valid Partial & Full Update Tests

    [Fact]
    public void Validate_WhenOnlyValidTitleProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateCategoryCommand(Title: "Sıcak İçecekler", Image: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyValidImageProvided_ShouldNotHaveAnyValidationErrors()
    {
        var fileMock = CreateMockFile("new_image.png", 2 * 1024 * 1024);
        var command = new UpdateCategoryCommand(Title: null, Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenBothTitleAndImageAreValid_ShouldNotHaveAnyValidationErrors()
    {
        var fileMock = CreateMockFile("new_image.webp", 1024 * 1024);
        var command = new UpdateCategoryCommand(Title: "Soğuk İçecekler", Image: fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreateMockFile(string fileName, long length)
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns(fileName);
        fileMock.Length.Returns(length);
        return fileMock;
    }

    #endregion
}