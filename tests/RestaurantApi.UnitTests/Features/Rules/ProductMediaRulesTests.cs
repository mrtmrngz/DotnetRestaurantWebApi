using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.ProductMediaRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class ProductMediaRulesTests
{
    private readonly ILogger<ProductMediaRules> _loggerMock = Substitute.For<ILogger<ProductMediaRules>>();
    private readonly ProductMediaRules _sut;

    public ProductMediaRulesTests()
    {
        _sut = new ProductMediaRules(_loggerMock);
    }

    #region ShouldDeletedImageCountCantEqualCurrentImageCount Tests

    [Fact]
    public async Task ShouldDeletedImageCountCantEqualCurrentImageCount_WhenNoNewImageAndAllImagesDeleted_ShouldThrowUnprocessableEntityError()
    {
        Action act = () => _sut.ShouldDeletedImageCountCantEqualCurrentImageCount(0, 3, 3);

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("Ürün resimsiz olamaz.");
    }

    [Fact]
    public async Task ShouldDeletedImageCountCantEqualCurrentImageCount_WhenNewImageProvided_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldDeletedImageCountCantEqualCurrentImageCount(2, 3, 3);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ShouldDeletedImageCountCantEqualCurrentImageCount_WhenNotAllImagesDeleted_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldDeletedImageCountCantEqualCurrentImageCount(0, 2, 3);

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldDeletedImgCountMatchesPrdDeletedImgCount Tests

    [Fact]
    public async Task ShouldDeletedImgCountMatchesPrdDeletedImgCount_WhenCountsDoNotMatch_ShouldThrowBadRequestException()
    {
        Action act = () => _sut.ShouldDeletedImgCountMatchesPrdDeletedImgCount(2, 3);

        act.Should().Throw<BadRequestException>()
            .WithMessage("Silinmek istenen görseller bu ürüne ait değil.");
    }

    [Fact]
    public async Task ShouldDeletedImgCountMatchesPrdDeletedImgCount_WhenCountsMatch_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldDeletedImgCountMatchesPrdDeletedImgCount(3, 3);

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldImageCountNotExceedMaximum Tests

    [Fact]
    public async Task ShouldImageCountNotExceedMaximum_WhenCountExceedsMaximum_ShouldThrowUnprocessableEntityError()
    {
        Action act = () => _sut.ShouldImageCountNotExceedMaximum(11);

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("Bir üründe en fazla 10 adet resim olabilir.");
    }

    [Fact]
    public async Task ShouldImageCountNotExceedMaximum_WhenCountIsAtMaximum_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldImageCountNotExceedMaximum(10);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ShouldImageCountNotExceedMaximum_WhenCountIsBelowMaximum_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldImageCountNotExceedMaximum(5);

        act.Should().NotThrow();
    }

    #endregion
}
