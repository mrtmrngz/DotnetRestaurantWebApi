
using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Cart.Commands.RemoveCartItemCommand;

namespace RestaurantApi.UnitTests.Features.Cart;

public class RemoveCartItemCommandValidatorTests
{
    private const string ItemIdRequiredMessage =
        "Sepet ürünü ID bilgisi zorunludur.";

    private const string InvalidRequestTypeMessage =
        "Geçersiz sepet işlem türü.";

    private const string PanelUserIdMessage =
        "Panel işlemlerinde kullanıcı ID bilgisi gönderilemez.";

    private const string PanelTableIdMessage =
        "Panel işlemlerinde geçerli bir masa ID bilgisi zorunludur.";

    private const string CurrentUserTableIdMessage =
        "Kullanıcı işlemlerinde masa ID bilgisi gönderilemez.";

    private const string CurrentUserIdMessage =
        "Kullanıcı işlemlerinde geçerli bir kullanıcı ID bilgisi zorunludur.";

    private readonly RemoveCartItemCommandValidator _validator = new();

    #region Helper Methods

    private static RemoveCartItemCommand CreateValidCurrentUserCommand(
        Guid? itemId = null)
    {
        return new RemoveCartItemCommand
        {
            ItemId = itemId ?? Guid.NewGuid(),
            RequestType = CartRequestTypes.CurrentUser,
            UserId = Guid.NewGuid(),
            RestaurantTableId = null
        };
    }

    private static RemoveCartItemCommand CreateValidPanelCommand(
        Guid? itemId = null)
    {
        return new RemoveCartItemCommand
        {
            ItemId = itemId ?? Guid.NewGuid(),
            RequestType = CartRequestTypes.PanelUser,
            UserId = null,
            RestaurantTableId = Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public void Validate_WhenCurrentUserRequestIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenPanelUserRequestIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = CreateValidPanelCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRequestTypeIsNotSpecified_ShouldDefaultToCurrentUser()
    {
        // Arrange
        var command = new RemoveCartItemCommand
        {
            ItemId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        command.RequestType.Should().Be(CartRequestTypes.CurrentUser);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ItemId Tests

    [Fact]
    public void Validate_WhenCurrentUserItemIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand(
            itemId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage(ItemIdRequiredMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelItemIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand(
            itemId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage(ItemIdRequiredMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenItemIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ItemId);
    }

    #endregion

    #region PanelUser Tests

    [Fact]
    public void Validate_WhenPanelUserHasValidTableId_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(
            x => x.RestaurantTableId);
    }

    [Fact]
    public void Validate_WhenPanelUserHasNullTableId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            RestaurantTableId = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasEmptyTableId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasUserId_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasEmptyUserId_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            UserId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage)
            .Only();
    }

    #endregion

    #region CurrentUser Tests

    [Fact]
    public void Validate_WhenCurrentUserHasValidUserId_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenCurrentUserHasNullUserId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            UserId = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasEmptyUserId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            UserId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasTableId_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            RestaurantTableId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasEmptyTableId_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage)
            .Only();
    }

    #endregion

    #region RequestType Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenRequestTypeIsInvalid_ShouldHaveValidationError(
        int requestType)
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            RequestType = (CartRequestTypes)requestType
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RequestType)
            .WithErrorMessage(InvalidRequestTypeMessage)
            .Only();
    }

    #endregion

    #region Multiple Property Validation Tests

    [Fact]
    public void Validate_WhenCurrentUserHasMultipleInvalidProperties_ShouldReturnAllErrors()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            ItemId = Guid.Empty,
            UserId = null,
            RestaurantTableId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage(ItemIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage);

        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Validate_WhenPanelUserHasMultipleInvalidProperties_ShouldReturnAllErrors()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            ItemId = Guid.Empty,
            UserId = Guid.NewGuid(),
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ItemId)
            .WithErrorMessage(ItemIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);

        result.Errors.Should().HaveCount(3);
    }

    #endregion

    #region Conditional Validation Tests

    [Fact]
    public void Validate_WhenPanelUserIsValid_ShouldNotApplyCurrentUserRules()
    {
        // Arrange
        var command = CreateValidPanelCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCurrentUserIsValid_ShouldNotApplyPanelUserRules()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
