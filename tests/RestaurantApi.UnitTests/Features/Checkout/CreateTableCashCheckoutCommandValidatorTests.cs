using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Checkout.Commands.CreateTableCashCheckoutCommand;

namespace RestaurantApi.UnitTests.Features.Checkout;

public class CreateTableCashCheckoutCommandValidatorTests
{
    private const int MaxIdempotencyKeyLength = 150;

    private const string TableIdRequiredMessage = "Masa ID bilgisi zorunludur ve boş olamaz.";
    private const string IdempotencyKeyRequiredMessage = "Idempotency key zorunludur.";
    private const string IdempotencyKeyLengthMessage = "Idempotency key en fazla 150 karakter olabilir.";

    private readonly CreateTableCashCheckoutCommandValidator _sut = new();

    #region Helper Methods

    private static CreateTableCashCheckoutCommand CreateValidCommand()
    {
        return new CreateTableCashCheckoutCommand
        {
            RestaurantTableId = Guid.NewGuid(),
            IdempotencyKey = "idempotency-key-1"
        };
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsExactlyMaxLength_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand() with { IdempotencyKey = new string('a', MaxIdempotencyKeyLength) };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region RestaurantTableId Tests

    [Fact]
    public void Validate_WhenRestaurantTableIdIsEmpty_ShouldHaveTableIdValidationError()
    {
        var command = CreateValidCommand() with { RestaurantTableId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(TableIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenRestaurantTableIdIsValid_ShouldNotHaveTableIdValidationError()
    {
        var result = _sut.TestValidate(CreateValidCommand());

        result.ShouldNotHaveValidationErrorFor(x => x.RestaurantTableId);
    }

    [Fact]
    public void Validate_WhenRestaurantTableIdIsEmpty_ShouldNotHaveIdempotencyKeyValidationError()
    {
        var command = CreateValidCommand() with { RestaurantTableId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
        result.Errors.Should().HaveCount(1, "yalnızca masa id hatası dönmeliydi.");
    }

    #endregion

    #region IdempotencyKey Tests

    [Fact]
    public void Validate_WhenIdempotencyKeyIsMissing_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { IdempotencyKey = null! };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsEmpty_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { IdempotencyKey = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsWhitespace_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCommand() with { IdempotencyKey = "   " };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var command = CreateValidCommand() with
        {
            IdempotencyKey = new string('a', MaxIdempotencyKeyLength + 1)
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyLengthMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsMissing_ShouldNotHaveTableIdValidationError()
    {
        var command = CreateValidCommand() with { IdempotencyKey = null! };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RestaurantTableId);
        result.Errors.Should().HaveCount(1, "yalnızca idempotency key hatası dönmeliydi.");
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WhenCommandIsEmpty_ShouldHaveTableIdAndIdempotencyKeyValidationErrors()
    {
        var result = _sut.TestValidate(new CreateTableCashCheckoutCommand());

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(TableIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
        result.Errors.Should().HaveCount(2, "masa id boş ve idempotency key gönderilmediği için iki hata dönmeliydi.");
    }

    #endregion
}
