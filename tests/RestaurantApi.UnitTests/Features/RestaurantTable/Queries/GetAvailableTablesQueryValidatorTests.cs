using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;

namespace RestaurantApi.UnitTests.Features.RestaurantTable.Queries;

public class GetAvailableTablesQueryValidatorTests
{
    private const string GuestCountRequiredMessage = "Misafir sayısı zorunludur.";
    private const string GuestCountRangeMessage = "Misafir sayısı 0'dan büyük olmalıdır.";
    private const string StartTimeRequiredMessage = "Başlangıç zamanı zorunludur.";
    private const string EndTimeRequiredMessage = "Bitiş zamanı zorunludur.";

    private readonly GetAvailableTablesQueryValidator _validator;

    public GetAvailableTablesQueryValidatorTests()
    {
        _validator = new GetAvailableTablesQueryValidator();
    }

    #region Helper Methods

    private static GetAvailableTablesQuery CreateValidQuery()
    {
        return new GetAvailableTablesQuery
        {
            GuestCount = 4,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(3)
        };
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.TestValidate(CreateValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(20)]
    public void Validate_WhenGuestCountIsGreaterThanZero_ShouldNotHaveValidationError(int guestCount)
    {
        var result = _validator.TestValidate(CreateValidQuery() with { GuestCount = guestCount });

        result.ShouldNotHaveValidationErrorFor(x => x.GuestCount);
    }

    #endregion

    #region GuestCount Tests

    [Fact]
    public void Validate_WhenGuestCountIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { GuestCount = 0 });

        result.ShouldHaveValidationErrorFor(x => x.GuestCount)
              .WithErrorMessage(GuestCountRequiredMessage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_WhenGuestCountIsNegative_ShouldHaveRangeValidationError(int guestCount)
    {
        var result = _validator.TestValidate(CreateValidQuery() with { GuestCount = guestCount });

        result.ShouldHaveValidationErrorFor(x => x.GuestCount)
              .WithErrorMessage(GuestCountRangeMessage);
    }

    #endregion

    #region StartTime Tests

    [Fact]
    public void Validate_WhenStartTimeIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { StartTime = default });

        result.ShouldHaveValidationErrorFor(x => x.StartTime)
              .WithErrorMessage(StartTimeRequiredMessage);
    }

    [Fact]
    public void Validate_WhenStartTimeIsProvided_ShouldNotHaveValidationError()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { StartTime = DateTime.UtcNow.AddHours(5) });

        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    #endregion

    #region EndTime Tests

    [Fact]
    public void Validate_WhenEndTimeIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { EndTime = default });

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
              .WithErrorMessage(EndTimeRequiredMessage);
    }

    [Fact]
    public void Validate_WhenEndTimeIsProvided_ShouldNotHaveValidationError()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { EndTime = DateTime.UtcNow.AddHours(6) });

        result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WhenAllFieldsAreMissing_ShouldHaveAllValidationErrors()
    {
        var result = _validator.TestValidate(new GetAvailableTablesQuery());

        result.Errors.Should().Contain(e => e.ErrorMessage == GuestCountRequiredMessage,
            "Misafir sayısı gönderilmediği için hata dönmeliydi.");
        result.Errors.Should().Contain(e => e.ErrorMessage == StartTimeRequiredMessage,
            "Başlangıç zamanı gönderilmediği için hata dönmeliydi.");
        result.Errors.Should().Contain(e => e.ErrorMessage == EndTimeRequiredMessage,
            "Bitiş zamanı gönderilmediği için hata dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyGuestCountIsInvalid_ShouldNotReturnDateErrors()
    {
        var result = _validator.TestValidate(CreateValidQuery() with { GuestCount = 0 });

        result.ShouldHaveValidationErrorFor(x => x.GuestCount);
        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
        result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
    }

    #endregion
}
