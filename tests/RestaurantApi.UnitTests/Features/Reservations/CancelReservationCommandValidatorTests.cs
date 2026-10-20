using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Reservation.Commands.CancelReservationCommand;

namespace RestaurantApi.UnitTests.Features.Reservations;

public class CancelReservationCommandValidatorTests
{
    private const string ReservationIdRequiredMessage = "Rezervasyon id boş olamaz.";
    private const string AdminUserIdNullMessage = "Admin tarafından yapılan iptal işlemlerinde kullanıcı id gönderilemez.";
    private const string CurrentUserUserIdRequiredMessage = "Kullanıcı tarafından yapılan iptal işlemlerinde kullanıcı id boş olamaz.";

    #region Helper Methods

    private static CancelReservationCommandValidator CreateValidator()
    {
        return new CancelReservationCommandValidator();
    }

    private static TestValidationResult<CancelReservationCommand> Validate(
        Guid reservationId,
        CancelReservationTypes requestType,
        Guid? userId = null)
    {
        var command = new CancelReservationCommand(
            ReservationId: reservationId,
            UserId: userId,
            RequestType: requestType
        );

        return CreateValidator().TestValidate(command);
    }

    #endregion

    #region ReservationId Tests

    [Fact]
    public void Validate_WhenReservationIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        var result = Validate(
            reservationId: Guid.Empty,
            requestType: CancelReservationTypes.Admin);

        result.ShouldHaveValidationErrorFor(x => x.ReservationId)
            .WithErrorMessage(ReservationIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenReservationIdIsValid_ShouldNotHaveValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.Admin);

        result.ShouldNotHaveValidationErrorFor(x => x.ReservationId);
    }

    #endregion

    #region Admin RequestType Tests

    [Fact]
    public void Validate_WhenAdminRequestContainsUserId_ShouldHaveUserIdNullValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.Admin,
            userId: Guid.NewGuid());

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(AdminUserIdNullMessage);
    }

    [Fact]
    public void Validate_WhenAdminRequestHasNullUserId_ShouldNotHaveValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.Admin,
            userId: null);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region CurrentUser RequestType Tests

    [Fact]
    public void Validate_WhenCurrentUserRequestHasNullUserId_ShouldHaveUserIdRequiredValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.CurrentUser,
            userId: null);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestHasEmptyUserId_ShouldHaveUserIdRequiredValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.CurrentUser,
            userId: Guid.Empty);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestHasValidUserId_ShouldNotHaveValidationError()
    {
        var result = Validate(
            reservationId: Guid.NewGuid(),
            requestType: CancelReservationTypes.CurrentUser,
            userId: Guid.NewGuid());

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}