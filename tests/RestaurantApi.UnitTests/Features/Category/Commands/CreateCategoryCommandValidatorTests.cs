using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;

namespace RestaurantApi.UnitTests.Features.Category.Commands;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandValidatorTests()
    {
        _validator = new CreateCategoryCommandValidator();
    }

    #region Title Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTitleIsEmpty_ShouldHaveValidationError(string? invalidTitle)
    {
        var command = new CreateCategoryCommand(invalidTitle!, Substitute.For<IFormFile>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenTitleIsLessThan2Characters_ShouldHaveValidationError()
    {
        var command = new CreateCategoryCommand("A", Substitute.For<IFormFile>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Kategori adı en az 2 karakter olmalıdır.");
    }

    [Fact]
    public void Validate_WhenTitleExceeds100Characters_ShouldHaveValidationError()
    {
        var command = new CreateCategoryCommand(new string('A', 101), Substitute.For<IFormFile>());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Kategori adı en fazla 100 karakter olabilir.");
    }

    #endregion

    #region Image Null Tests

    [Fact]
    public void Validate_WhenImageIsNull_ShouldHaveValidationError()
    {
        var command = new CreateCategoryCommand("Tatlılar", null!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Image)
              .WithErrorMessage("Kategori görseli yüklemek zorunludur.");
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
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns(fileName);
        fileMock.Length.Returns(1024);

        var command = new CreateCategoryCommand("Çorbalar", fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Image);
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("script.exe")]
    [InlineData("image.gif")]
    [InlineData("noextension")]
    public void Validate_WhenImageHasInvalidExtension_ShouldHaveValidationError(string fileName)
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns(fileName);
        fileMock.Length.Returns(1024);

        var command = new CreateCategoryCommand("Çorbalar", fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Image)
              .WithErrorMessage("Sadece .jpg, .jpeg, .png veya .webp formatında görseller yüklenebilir.");
    }

    #endregion

    #region Image Size Tests

    [Fact]
    public void Validate_WhenImageExceeds5MB_ShouldHaveValidationError()
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns("large-image.png");
        fileMock.Length.Returns(5 * 1024 * 1024 + 1);

        var command = new CreateCategoryCommand("Ana Yemekler", fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Image)
              .WithErrorMessage("Görsel boyutu en fazla 5MB olabilir.");
    }

    [Fact]
    public void Validate_WhenImageIsExactly5MB_ShouldNotHaveSizeError()
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns("valid-image.png");
        fileMock.Length.Returns(5 * 1024 * 1024);

        var command = new CreateCategoryCommand("Ana Yemekler", fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Image);
    }

    #endregion

    #region Valid Model Test

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns("valid_category.png");
        fileMock.Length.Returns(2 * 1024 * 1024);

        var command = new CreateCategoryCommand("Başlangıçlar", fileMock);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}