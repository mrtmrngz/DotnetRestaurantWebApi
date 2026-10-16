using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;

namespace RestaurantApi.UnitTests.Features.RestaurantTable.Commands;

public class CreateRestaurantTableCommandValidatorTests
{
    private const string InvalidNumberMessage = "Masa numarası 0'dan büyük olmalıdır.";
    private const string InvalidCapacityMessage = "Masa kapasitesi 0'dan büyük olmalıdır.";
    private const string CapacityAboveMaxMessage = "Masa kapasitesi 15'ten küçük olmalıdır.";

    private readonly CreateRestaurantTableCommandValidator _validator;

    public CreateRestaurantTableCommandValidatorTests()
    {
        _validator = new CreateRestaurantTableCommandValidator();
    }

    #region Helper Methods

    private static CreateRestaurantTableCommand CreateValidCommand()
    {
        return new CreateRestaurantTableCommand(Number: 5, Capacity: 4);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 7)]
    [InlineData(50, 14)]
    public void Validate_WhenNumberAndCapacityAreValid_ShouldNotHaveAnyValidationErrors(int number, int capacity)
    {
        var command = new CreateRestaurantTableCommand(Number: number, Capacity: capacity);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Number Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenNumberIsNotGreaterThanZero_ShouldHaveValidationError(int invalidNumber)
    {
        var command = CreateValidCommand() with { Number = invalidNumber };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Number)
              .WithErrorMessage(InvalidNumberMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(500)]
    public void Validate_WhenNumberIsGreaterThanZero_ShouldNotHaveValidationError(int validNumber)
    {
        var command = CreateValidCommand() with { Number = validNumber };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Number);
    }

    #endregion

    #region Capacity Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_WhenCapacityIsNotGreaterThanZero_ShouldHaveValidationError(int invalidCapacity)
    {
        var command = CreateValidCommand() with { Capacity = invalidCapacity };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Capacity)
              .WithErrorMessage(InvalidCapacityMessage);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(100)]
    public void Validate_WhenCapacityIsNotLessThanFifteen_ShouldHaveValidationError(int invalidCapacity)
    {
        var command = CreateValidCommand() with { Capacity = invalidCapacity };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Capacity)
              .WithErrorMessage(CapacityAboveMaxMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void Validate_WhenCapacityIsWithinRange_ShouldNotHaveValidationError(int validCapacity)
    {
        var command = CreateValidCommand() with { Capacity = validCapacity };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WhenNumberAndCapacityAreInvalid_ShouldHaveBothValidationErrors()
    {
        var command = new CreateRestaurantTableCommand(Number: 0, Capacity: 20);

        var result = _validator.TestValidate(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == InvalidNumberMessage,
            "Masa numarası 0 olduğu için numara hatası dönmeliydi.");
        result.Errors.Should().Contain(e => e.ErrorMessage == CapacityAboveMaxMessage,
            "Kapasite 15'ten küçük olmadığı için kapasite hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyNumberIsInvalid_ShouldHaveOnlyNumberValidationError()
    {
        var command = new CreateRestaurantTableCommand(Number: 0, Capacity: 4);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Number);
        result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
    }

    #endregion
}
