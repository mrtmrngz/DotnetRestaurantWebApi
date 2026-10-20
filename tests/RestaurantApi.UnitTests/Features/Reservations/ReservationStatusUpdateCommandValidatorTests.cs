using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;
using RestaurantApi.Domain.Entities;

namespace RestaurantApi.UnitTests.Features.Reservations;

public class ReservationStatusUpdateCommandValidatorTests
{
    private const string ReservationIdRequiredMessage =
        "Rezervasyon ID bilgisi zorunludur.";

    private const string StatusRequiredMessage =
        "Rezervasyon durumu zorunludur.";

    private const string StatusInvalidMessage =
        "Geçersiz rezervasyon durumu.";

    private readonly ReservationStatusUpdateCommandValidator _validator = new();

    #region Helper Methods

    private static ReservationStatusUpdateCommand CreateCommand(
        string status = ReservationStatus.Pending,
        Guid? reservationId = null)
    {
        return new ReservationStatusUpdateCommand(status)
        {
            ReservationId = reservationId ?? Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Request Tests

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Completed)]
    [InlineData(ReservationStatus.Canceled)]
    public void Validate_WhenStatusIsValid_ShouldNotHaveAnyValidationErrors(
        string status)
    {
        // Arrange
        var command = CreateCommand(status);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = CreateCommand(
            status: ReservationStatus.Confirmed,
            reservationId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ReservationId Tests

    [Fact]
    public void Validate_WhenReservationIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateCommand(
            reservationId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReservationId)
            .WithErrorMessage(ReservationIdRequiredMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenReservationIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateCommand(
            reservationId: Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReservationId);
    }

    #endregion

    #region Status Required Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Validate_WhenStatusIsEmpty_ShouldHaveRequiredValidationError(
        string? status)
    {
        // Arrange
        var command = CreateCommand(status!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusRequiredMessage)
            .Only();
    }

    #endregion

    #region Invalid Status Tests

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Approved")]
    [InlineData("Unknown")]
    [InlineData("123")]
    [InlineData("InvalidStatus")]
    public void Validate_WhenStatusIsNotDefined_ShouldHaveInvalidStatusValidationError(
        string status)
    {
        // Arrange
        var command = CreateCommand(status);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusInvalidMessage)
            .Only();
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("PENDING")]
    [InlineData("confirmed")]
    [InlineData("CONFIRMED")]
    [InlineData("completed")]
    [InlineData("COMPLETED")]
    [InlineData("canceled")]
    [InlineData("CANCELED")]
    public void Validate_WhenStatusHasIncorrectCase_ShouldHaveInvalidStatusValidationError(
        string status)
    {
        // Arrange
        var command = CreateCommand(status);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusInvalidMessage)
            .Only();
    }

    [Theory]
    [InlineData(" Pending")]
    [InlineData("Pending ")]
    [InlineData(" Confirmed ")]
    [InlineData("\tCompleted")]
    [InlineData("Canceled\n")]
    public void Validate_WhenStatusContainsExtraWhitespace_ShouldHaveInvalidStatusValidationError(
        string status)
    {
        // Arrange
        var command = CreateCommand(status);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusInvalidMessage)
            .Only();
    }

    #endregion

    #region CascadeMode Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenStatusIsEmpty_ShouldStopValidationAfterNotEmpty(
        string? status)
    {
        // Arrange
        var command = CreateCommand(status!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(StatusRequiredMessage);
    }

    #endregion

    #region Multiple Property Validation Tests

    [Fact]
    public void Validate_WhenBothPropertiesAreInvalid_ShouldReturnBothErrors()
    {
        // Arrange
        var command = CreateCommand(
            status: "Rejected",
            reservationId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReservationId)
            .WithErrorMessage(ReservationIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusInvalidMessage);

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_WhenStatusIsEmptyAndReservationIdIsInvalid_ShouldReturnTwoErrors()
    {
        // Arrange
        var command = CreateCommand(
            status: "",
            reservationId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReservationId)
            .WithErrorMessage(ReservationIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage(StatusRequiredMessage);

        result.Errors.Should().HaveCount(2);
    }

    #endregion
}
