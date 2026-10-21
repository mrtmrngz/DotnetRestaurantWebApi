
using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;

namespace RestaurantApi.UnitTests.Features.Reservations;

public class ReservationListQueryValidatorTests
{
    private const string InvalidRequestTypeMessage =
        "Geçersiz rezervasyon listeleme türü.";

    private const string PanelUserIdMessage =
        "Panel kullanıcısı tarafından yapılan listeleme işlemlerinde kullanıcı ID gönderilemez.";

    private const string CurrentUserIdMessage =
        "Kullanıcı tarafından yapılan listeleme işlemlerinde kullanıcı ID boş olamaz.";

    private readonly ReservationListQueryValidator _validator = new();

    #region Helper Methods

    private TestValidationResult<ReservationListQuery> Validate(
        ReservationListRequestEnums requestType,
        Guid? userId)
    {
        var query = new ReservationListQuery(
            UserId: userId,
            RequestType: requestType);

        return _validator.TestValidate(query);
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public void Validate_WhenPanelUserHasNoUserId_ShouldNotHaveAnyValidationErrors()
    {
        var result = Validate(
            ReservationListRequestEnums.PanelUser,
            null);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasValidUserId_ShouldNotHaveAnyValidationErrors()
    {
        var result = Validate(
            ReservationListRequestEnums.CurrentUser,
            Guid.NewGuid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRequestTypeIsDefault_ShouldValidateAsCurrentUser()
    {
        var query = new ReservationListQuery(Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PanelUser Tests

    [Fact]
    public void Validate_WhenPanelUserHasValidUserId_ShouldHaveValidationError()
    {
        var result = Validate(
            ReservationListRequestEnums.PanelUser,
            Guid.NewGuid());

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasEmptyGuid_ShouldHaveValidationError()
    {
        var result = Validate(
            ReservationListRequestEnums.PanelUser,
            Guid.Empty);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage)
            .Only();
    }

    #endregion

    #region CurrentUser Tests

    [Fact]
    public void Validate_WhenCurrentUserHasNullUserId_ShouldHaveValidationError()
    {
        var result = Validate(
            ReservationListRequestEnums.CurrentUser,
            null);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasEmptyGuid_ShouldHaveValidationError()
    {
        var result = Validate(
            ReservationListRequestEnums.CurrentUser,
            Guid.Empty);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasValidUserId_ShouldNotHaveUserIdValidationError()
    {
        var result = Validate(
            ReservationListRequestEnums.CurrentUser,
            Guid.NewGuid());

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region RequestType Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenRequestTypeIsUndefined_ShouldHaveValidationError(
        int requestType)
    {
        var result = Validate(
            (ReservationListRequestEnums)requestType,
            Guid.NewGuid());

        result.ShouldHaveValidationErrorFor(x => x.RequestType)
            .WithErrorMessage(InvalidRequestTypeMessage)
            .Only();
    }

    #endregion

    #region Conditional Validation Tests

    [Fact]
    public void Validate_WhenPanelUserHasNullUserId_ShouldNotApplyCurrentUserRule()
    {
        var result = Validate(
            ReservationListRequestEnums.PanelUser,
            null);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenCurrentUserHasValidUserId_ShouldNotApplyPanelUserRule()
    {
        var result = Validate(
            ReservationListRequestEnums.CurrentUser,
            Guid.NewGuid());

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Validate_WhenRequestTypeIsInvalid_ShouldOnlyReturnRequestTypeError(
        int requestType)
    {
        var result = Validate(
            (ReservationListRequestEnums)requestType,
            null);

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(
                nameof(ReservationListQuery.RequestType));
    }

    #endregion
}
