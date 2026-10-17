using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.RestaurantTable.Commands.UpdateRestaurantTableCommand;

namespace RestaurantApi.UnitTests.Features.RestaurantTable.Commands;

public class UpdateRestaurantTableCommandValidatorTests
{
    private const string AtLeastOneFieldMessage = "Güncellenecek en az bir alan belirtilmelidir.";
    private const string InvalidNumberMessage = "Masa numarası 0'dan büyük olmalıdır.";
    private const string InvalidCapacityMessage = "Masa kapasitesi 0'dan büyük olmalıdır.";
    private const string CapacityAboveMaxMessage = "Masa kapasitesi 15'ten küçük olmalıdır.";

    private readonly UpdateRestaurantTableCommandValidator _validator;

    public UpdateRestaurantTableCommandValidatorTests()
    {
        _validator = new UpdateRestaurantTableCommandValidator();
    }

    #region Helper Methods

    private static UpdateRestaurantTableCommand CreateValidCommand()
    {
        return new UpdateRestaurantTableCommand(Number: 5, Capacity: 4, IsActive: true);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenAllFieldsAreProvided_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyNumberIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(Number: 5));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyCapacityIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(Capacity: 4));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyIsActiveIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(IsActive: true));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIsActiveIsFalse_ShouldNotHaveAtLeastOneFieldError()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(IsActive: false));

        result.Errors.Should().NotContain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "IsActive false olarak gönderilse de güncellenecek bir alan vardır.");
    }

    #endregion

    #region At Least One Field Tests

    [Fact]
    public void Validate_WhenAllFieldsAreNull_ShouldHaveAtLeastOneFieldError()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand());

        result.Errors.Should().Contain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "Masa id dışında hiçbir alan gönderilmediği için hata dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenNumberIsZero_ShouldNotHaveAtLeastOneFieldError()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(Number: 0));

        result.Errors.Should().NotContain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "Number 0 olarak gönderilse de güncellenecek bir alan vardır.");
    }

    #endregion

    #region Number Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WhenNumberIsNotGreaterThanZero_ShouldHaveValidationError(int invalidNumber)
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Number = invalidNumber });

        result.ShouldHaveValidationErrorFor(x => x.Number)
              .WithErrorMessage(InvalidNumberMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(500)]
    public void Validate_WhenNumberIsGreaterThanZero_ShouldNotHaveValidationError(int validNumber)
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Number = validNumber });

        result.ShouldNotHaveValidationErrorFor(x => x.Number);
    }

    [Fact]
    public void Validate_WhenNumberIsNull_ShouldNotHaveValidationError()
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Number = null });

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
        var result = _validator.TestValidate(CreateValidCommand() with { Capacity = invalidCapacity });

        result.ShouldHaveValidationErrorFor(x => x.Capacity)
              .WithErrorMessage(InvalidCapacityMessage);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(100)]
    public void Validate_WhenCapacityIsNotLessThanFifteen_ShouldHaveValidationError(int invalidCapacity)
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Capacity = invalidCapacity });

        result.ShouldHaveValidationErrorFor(x => x.Capacity)
              .WithErrorMessage(CapacityAboveMaxMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    public void Validate_WhenCapacityIsWithinRange_ShouldNotHaveValidationError(int validCapacity)
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Capacity = validCapacity });

        result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
    }

    [Fact]
    public void Validate_WhenCapacityIsNull_ShouldNotHaveValidationError()
    {
        var result = _validator.TestValidate(CreateValidCommand() with { Capacity = null });

        result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WhenNumberAndCapacityAreInvalid_ShouldHaveBothValidationErrors()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(Number: 0, Capacity: 20));

        result.Errors.Should().Contain(e => e.ErrorMessage == InvalidNumberMessage,
            "Masa numarası 0 olduğu için numara hatası dönmeliydi.");
        result.Errors.Should().Contain(e => e.ErrorMessage == CapacityAboveMaxMessage,
            "Kapasite 15'ten küçük olmadığı için kapasite hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyNumberIsInvalid_ShouldHaveOnlyNumberValidationError()
    {
        var result = _validator.TestValidate(new UpdateRestaurantTableCommand(Number: 0, Capacity: 4));

        result.ShouldHaveValidationErrorFor(x => x.Number);
        result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
    }

    #endregion
}
