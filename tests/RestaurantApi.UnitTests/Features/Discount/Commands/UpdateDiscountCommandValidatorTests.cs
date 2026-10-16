using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Discount.Commands.UpdateDiscountCommand;

namespace RestaurantApi.UnitTests.Features.Discount.Commands;

public class UpdateDiscountCommandValidatorTests
{
    private readonly UpdateDiscountCommandValidator _validator;

    public UpdateDiscountCommandValidatorTests()
    {
        _validator = new UpdateDiscountCommandValidator();
    }

    private const string AtLeastOneFieldMessage = "Güncellenecek en az bir alan belirtilmelidir.";


    private static UpdateDiscountCommand CreateValidCommand()
    {
        return new UpdateDiscountCommand(
            Rate: 15,
            StartDate: DateTime.UtcNow.AddDays(1),
            EndDate: DateTime.UtcNow.AddDays(3),
            IsActive: true,
            ProductId: Guid.NewGuid()
        );
    }

    [Fact]
    public void Validate_WhenAllFieldsAreProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyRateIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateDiscountCommand(Rate: 15);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyStartDateIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateDiscountCommand(StartDate: DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyEndDateIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateDiscountCommand(EndDate: DateTime.UtcNow.AddDays(3));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyIsActiveIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateDiscountCommand(IsActive: true);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyProductIdIsProvided_ShouldNotHaveAnyValidationErrors()
    {
        var command = new UpdateDiscountCommand(ProductId: Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAllFieldsAreNull_ShouldHaveAtLeastOneFieldError()
    {
        var command = new UpdateDiscountCommand();

        var result = _validator.TestValidate(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "İndirim id dışında hiçbir alan gönderilmediği için hata dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenIsActiveIsFalse_ShouldNotHaveAtLeastOneFieldError()
    {
        var command = new UpdateDiscountCommand(IsActive: false);

        var result = _validator.TestValidate(command);

        result.Errors.Should().NotContain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "IsActive false olarak gönderilse de güncellenecek bir alan vardır.");
    }

    [Fact]
    public void Validate_WhenRateIsZero_ShouldNotHaveAtLeastOneFieldError()
    {
        var command = new UpdateDiscountCommand(Rate: 0);

        var result = _validator.TestValidate(command);

        result.Errors.Should().NotContain(e => e.ErrorMessage == AtLeastOneFieldMessage,
            "Rate 0 olarak gönderilse de güncellenecek bir alan vardır.");
    }


    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(0.09)]
    public void Validate_WhenRateIsBelowMinimum_ShouldHaveValidationError(double invalidRate)
    {
        var command = CreateValidCommand() with { Rate = invalidRate };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Rate)
              .WithErrorMessage("İndirim oranı en az 0.1 olmalıdır.");
    }

    [Theory]
    [InlineData(99.91)]
    [InlineData(100.0)]
    [InlineData(150.0)]
    public void Validate_WhenRateIsAboveMaximum_ShouldHaveValidationError(double invalidRate)
    {
        var command = CreateValidCommand() with { Rate = invalidRate };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Rate)
              .WithErrorMessage("İndirim oranı en fazla 99.9 olabilir.");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(50.0)]
    [InlineData(99.9)]
    public void Validate_WhenRateIsWithinRange_ShouldNotHaveValidationError(double validRate)
    {
        var command = CreateValidCommand() with { Rate = validRate };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Rate);
    }

    [Fact]
    public void Validate_WhenRateIsNull_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { Rate = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Rate);
    }



    [Fact]
    public void Validate_WhenStartDateIsMinValue_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { StartDate = new DateTime() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StartDate)
              .WithErrorMessage("İndirim başlangıç tarihi boş olamaz.");
    }

    [Fact]
    public void Validate_WhenStartDateIsNull_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { StartDate = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void Validate_WhenStartDateIsValid_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { StartDate = DateTime.UtcNow.AddDays(2) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.StartDate);
    }


    [Fact]
    public void Validate_WhenEndDateIsMinValue_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { EndDate = new DateTime() };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.EndDate)
              .WithErrorMessage("İndirim bitiş tarihi boş olamaz.");
    }

    [Fact]
    public void Validate_WhenEndDateIsNull_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { EndDate = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_WhenEndDateIsValid_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { EndDate = DateTime.UtcNow.AddDays(5) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }


    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProductId)
              .WithErrorMessage("Ürün id boş olamaz.");
    }

    [Fact]
    public void Validate_WhenProductIdIsNull_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { ProductId = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void Validate_WhenProductIdIsValid_ShouldNotHaveValidationError()
    {
        var command = CreateValidCommand() with { ProductId = Guid.NewGuid() };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }
    
}
