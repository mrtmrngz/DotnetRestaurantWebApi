using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Checkout.Commands.CreateOnlineCheckoutCommand;

namespace RestaurantApi.UnitTests.Features.Checkout;

public class CreateOnlineCheckoutCommandValidatorTests
{
    private const string IdempotencyKeyRequiredMessage = "Idempotency key zorunludur.";
    private const string IdempotencyKeyLengthMessage = "Idempotency key en fazla 150 karakter olabilir.";
    private const string InvalidRequestTypeMessage = "Geçersiz ödeme işlem türü.";
    private const string CurrentUserUserIdMessage =
        "Kullanıcı işlemlerinde geçerli bir kullanıcı ID bilgisi zorunludur.";
    private const string CurrentUserTableIdMessage = "Kullanıcı işlemlerinde masa ID bilgisi gönderilemez.";
    private const string PanelUserIdMessage = "Panel işlemlerinde kullanıcı ID bilgisi gönderilemez.";
    private const string PanelTableIdMessage = "Panel işlemlerinde geçerli bir masa ID bilgisi zorunludur.";

    private readonly CreateOnlineCheckoutCommandValidator _sut = new();

    #region Helper Methods

    private static CreateOnlineCheckoutCommand CreateValidCurrentUserCommand()
    {
        return new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.CurrentUser,
            UserId = Guid.NewGuid(),
            RestaurantTableId = null,
            IdempotencyKey = "idempotency-key-1"
        };
    }

    private static CreateOnlineCheckoutCommand CreateValidPanelUserCommand()
    {
        return new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = CheckoutRequestTypes.PanelUser,
            RestaurantTableId = Guid.NewGuid(),
            UserId = null,
            IdempotencyKey = "idempotency-key-2"
        };
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public void Validate_WhenCurrentUserCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidCurrentUserCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenPanelUserCommandIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidPanelUserCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRequestTypeIsNotSet_ShouldUseCurrentUserRules()
    {
        // varsayılan tip CurrentUser olduğu için yalnızca kullanıcı id ve idempotency key yeterlidir
        var command = new CreateOnlineCheckoutCommand
        {
            UserId = Guid.NewGuid(),
            IdempotencyKey = "idempotency-key-3"
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region IdempotencyKey Tests

    [Fact]
    public void Validate_WhenIdempotencyKeyIsMissing_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { IdempotencyKey = null! };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsEmpty_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { IdempotencyKey = string.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsWhitespace_ShouldHaveRequiredValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { IdempotencyKey = "   " };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsExactlyMaxLength_ShouldNotHaveValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { IdempotencyKey = new string('a', 150) };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.IdempotencyKey);
    }

    [Fact]
    public void Validate_WhenIdempotencyKeyIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { IdempotencyKey = new string('a', 151) };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyLengthMessage);
    }

    #endregion

    #region CheckoutRequestTypes Tests

    [Theory]
    [InlineData(CheckoutRequestTypes.CurrentUser)]
    [InlineData(CheckoutRequestTypes.PanelUser)]
    public void Validate_WhenRequestTypeIsDefined_ShouldNotHaveEnumValidationError(
        CheckoutRequestTypes requestType)
    {
        var command = requestType == CheckoutRequestTypes.CurrentUser
            ? CreateValidCurrentUserCommand()
            : CreateValidPanelUserCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.CheckoutRequestTypes);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public void Validate_WhenRequestTypeIsNotDefined_ShouldHaveEnumValidationError(int undefinedRequestType)
    {
        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = (CheckoutRequestTypes)undefinedRequestType,
            UserId = Guid.NewGuid(),
            IdempotencyKey = "idempotency-key-4"
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CheckoutRequestTypes)
            .WithErrorMessage(InvalidRequestTypeMessage);
    }

    [Fact]
    public void Validate_WhenRequestTypeIsNotDefined_ShouldNotApplyTypeSpecificRules()
    {
        var command = new CreateOnlineCheckoutCommand
        {
            CheckoutRequestTypes = (CheckoutRequestTypes)99,
            UserId = null,
            RestaurantTableId = Guid.NewGuid(),
            IdempotencyKey = "idempotency-key-5"
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CheckoutRequestTypes);
        result.Errors.Should().HaveCount(1, "tip geçersiz olduğu için kullanıcı/masa kuralları çalışmamalıydı.");
    }

    #endregion

    #region CurrentUser Tests

    [Fact]
    public void Validate_WhenCurrentUserRequestHasNoUserId_ShouldHaveUserIdValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { UserId = null };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdMessage);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestHasEmptyUserId_ShouldHaveUserIdValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdMessage);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestHasRestaurantTableId_ShouldHaveTableIdValidationError()
    {
        var command = CreateValidCurrentUserCommand() with { RestaurantTableId = Guid.NewGuid() };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestHasNoUserIdAndHasTableId_ShouldHaveBothValidationErrors()
    {
        var command = CreateValidCurrentUserCommand() with { UserId = null, RestaurantTableId = Guid.NewGuid() };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdMessage);
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_WhenCurrentUserRequestIsValid_ShouldNotHaveTableIdValidationError()
    {
        var result = _sut.TestValidate(CreateValidCurrentUserCommand());

        result.ShouldNotHaveValidationErrorFor(x => x.RestaurantTableId);
    }

    #endregion

    #region PanelUser Tests

    [Fact]
    public void Validate_WhenPanelUserRequestHasNoTableId_ShouldHaveTableIdValidationError()
    {
        var command = CreateValidPanelUserCommand() with { RestaurantTableId = null };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);
    }

    [Fact]
    public void Validate_WhenPanelUserRequestHasEmptyTableId_ShouldHaveTableIdValidationError()
    {
        var command = CreateValidPanelUserCommand() with { RestaurantTableId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);
    }

    [Fact]
    public void Validate_WhenPanelUserRequestHasUserId_ShouldHaveUserIdValidationError()
    {
        var command = CreateValidPanelUserCommand() with { UserId = Guid.NewGuid() };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage);
    }

    [Fact]
    public void Validate_WhenPanelUserRequestHasNoTableIdAndHasUserId_ShouldHaveBothValidationErrors()
    {
        var command = CreateValidPanelUserCommand() with { RestaurantTableId = null, UserId = Guid.NewGuid() };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage);
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_WhenPanelUserRequestIsValid_ShouldNotHaveUserIdValidationError()
    {
        var result = _sut.TestValidate(CreateValidPanelUserCommand());

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenPanelUserRequestHasNoUserId_ShouldNotReportUserIdValidationError()
    {
        // panel tipinde kullanıcı id'si boş olabilir (zorunlu olan masa id'dir)
        var command = CreateValidPanelUserCommand() with { UserId = null };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_WhenCommandIsEmpty_ShouldHaveKeyAndUserIdValidationErrors()
    {
        var result = _sut.TestValidate(new CreateOnlineCheckoutCommand());

        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage(IdempotencyKeyRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserUserIdMessage);
        result.Errors.Should().HaveCount(2, "varsayılan tip CurrentUser olduğu için iki hata dönmeliydi.");
    }

    #endregion
}
