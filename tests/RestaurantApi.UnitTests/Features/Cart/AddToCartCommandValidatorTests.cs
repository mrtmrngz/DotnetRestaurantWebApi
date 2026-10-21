
using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Cart.Commands.AddToCartCommand;

namespace RestaurantApi.UnitTests.Features.Cart;

public class AddToCartCommandValidatorTests
{
    private const string ProductIdRequiredMessage =
        "Ürün ID bilgisi zorunludur.";

    private const string QuantityRangeMessage =
        "Ürün miktarı 1 ile 100 arasında olmak zorundadır.";

    private const string PanelUserIdMessage =
        "Panel işlemlerinde kullanıcı ID bilgisi gönderilemez.";

    private const string PanelTableIdMessage =
        "Panel işlemlerinde geçerli bir masa ID bilgisi zorunludur.";

    private const string CurrentUserTableIdMessage =
        "Kullanıcı işlemlerinde masa ID bilgisi gönderilemez.";

    private const string CurrentUserIdMessage =
        "Kullanıcı işlemlerinde geçerli bir kullanıcı ID bilgisi zorunludur.";

    private readonly AddToCartCommandValidator _validator = new();

    #region Helper Methods

    private static AddToCartCommand CreateValidCurrentUserCommand(
        int quantity = 1,
        Guid? productId = null)
    {
        return new AddToCartCommand(
            ProductId: productId ?? Guid.NewGuid(),
            Quantity: quantity)
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = Guid.NewGuid(),
            RestaurantTableId = null
        };
    }

    private static AddToCartCommand CreateValidPanelCommand(
        int quantity = 1,
        Guid? productId = null)
    {
        return new AddToCartCommand(
            ProductId: productId ?? Guid.NewGuid(),
            Quantity: quantity)
        {
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
        var command = new AddToCartCommand(
            ProductId: Guid.NewGuid(),
            Quantity: 1)
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        command.RequestType.Should().Be(CartRequestTypes.CurrentUser);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ProductId Tests

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand(
            productId: Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage(ProductIdRequiredMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenProductIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region Quantity Tests

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Validate_WhenQuantityIsZeroOrNegative_ShouldHaveRangeValidationError(
        int quantity)
    {
        // Arrange
        var command = CreateValidCurrentUserCommand(quantity);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage(QuantityRangeMessage)
            .Only();
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenQuantityExceedsMaximum_ShouldHaveRangeValidationError(
        int quantity)
    {
        // Arrange
        var command = CreateValidCurrentUserCommand(quantity);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage(QuantityRangeMessage)
            .Only();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WhenQuantityIsWithinAllowedRange_ShouldNotHaveValidationError(
        int quantity)
    {
        // Arrange
        var command = CreateValidCurrentUserCommand(quantity);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_WhenPanelQuantityIsAtBoundary_ShouldNotHaveValidationError(
        int quantity)
    {
        // Arrange
        var command = CreateValidPanelCommand(quantity);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
        result.ShouldNotHaveValidationErrorFor(x => x.RestaurantTableId);
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

    #region Multiple Property Validation Tests

    [Fact]
    public void Validate_WhenCurrentUserHasMultipleInvalidProperties_ShouldReturnAllErrors()
    {
        // Arrange
        var command = CreateValidCurrentUserCommand() with
        {
            ProductId = Guid.Empty,
            Quantity = 0,
            UserId = null,
            RestaurantTableId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage(ProductIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage(QuantityRangeMessage);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage);

        result.Errors.Should().HaveCount(4);
    }

    [Fact]
    public void Validate_WhenPanelUserHasMultipleInvalidProperties_ShouldReturnAllErrors()
    {
        // Arrange
        var command = CreateValidPanelCommand() with
        {
            ProductId = Guid.Empty,
            Quantity = 101,
            UserId = Guid.NewGuid(),
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage(ProductIdRequiredMessage);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage(QuantityRangeMessage);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);

        result.Errors.Should().HaveCount(4);
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

    #region Enum Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void Validate_WhenRequestTypeIsInvalid_ShouldHaveValidationError(
        int requestType)
    {
        var command = CreateValidCurrentUserCommand() with
        {
            RequestType = (CartRequestTypes)requestType
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RequestType)
            .WithErrorMessage("Geçersiz sepet işlem türü.")
            .Only();
    }

    #endregion
}
