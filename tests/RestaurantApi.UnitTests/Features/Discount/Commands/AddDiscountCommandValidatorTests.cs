using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Discount.Commands.AddDiscountCommand;

namespace RestaurantApi.UnitTests.Features.Discount.Commands;

public class AddDiscountCommandValidatorTests
{
    private readonly AddDiscountCommandValidator _validator;

    public AddDiscountCommandValidatorTests()
    {
        _validator = new AddDiscountCommandValidator();
    }

    private static AddDiscountCommand CreateValidCommand()
    {
        return new AddDiscountCommand(
            ProductId: Guid.NewGuid(),
            DiscountRate: 15,
            StartDate: DateTime.UtcNow.AddDays(1),
            EndDate: DateTime.UtcNow.AddDays(3)
        );
    }

    #region Valid Command

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ProductId Tests

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProductId)
              .WithErrorMessage("Ürün id boş olamaz.");
    }

    #endregion

    #region DiscountRate Tests

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(0.09)]
    public void Validate_WhenDiscountRateIsBelowMinimum_ShouldHaveValidationError(double invalidRate)
    {
        var command = CreateValidCommand() with { DiscountRate = invalidRate };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DiscountRate)
              .WithErrorMessage("İndirim oranı en az 0.1 olmalıdır.");
    }

    [Theory]
    [InlineData(99.91)]
    [InlineData(100.0)]
    [InlineData(150.0)]
    public void Validate_WhenDiscountRateIsAboveMaximum_ShouldHaveValidationError(double invalidRate)
    {
        var command = CreateValidCommand() with { DiscountRate = invalidRate };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DiscountRate)
              .WithErrorMessage("İndirim oranı en fazla 99.9 olabilir.");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(50.0)]
    [InlineData(99.9)]
    public void Validate_WhenDiscountRateIsWithinRange_ShouldNotHaveValidationError(double validRate)
    {
        var command = CreateValidCommand() with { DiscountRate = validRate };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DiscountRate);
    }

    #endregion

    #region StartDate Tests

    [Fact]
    public void Validate_WhenStartDateIsDefault_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { StartDate = default };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StartDate)
              .WithErrorMessage("İndirim başlangıç tarihi zorunludur.");
    }

    #endregion

    #region EndDate Tests

    [Fact]
    public void Validate_WhenEndDateIsDefault_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { EndDate = default };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EndDate)
              .WithErrorMessage("İndirim bitiş tarihi zorunludur.");
    }

    #endregion
}
