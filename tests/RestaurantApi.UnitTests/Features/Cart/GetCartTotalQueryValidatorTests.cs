
using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Cart.Queries.GetCartTotalQuery;

namespace RestaurantApi.UnitTests.Features.Cart;

public class GetCartTotalQueryValidatorTests
{
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

    private readonly GetCartTotalQueryValidator _validator = new();

    #region Helper Methods

    private static GetCartTotalQuery CreateValidCurrentUserQuery()
    {
        return new GetCartTotalQuery
        {
            RequestType = CartRequestTypes.CurrentUser,
            UserId = Guid.NewGuid(),
            RestaurantTableId = null
        };
    }

    private static GetCartTotalQuery CreateValidPanelQuery()
    {
        return new GetCartTotalQuery
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
        var query = CreateValidCurrentUserQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenPanelUserRequestIsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var query = CreateValidPanelQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRequestTypeIsNotSpecified_ShouldDefaultToCurrentUser()
    {
        // Arrange
        var query = new GetCartTotalQuery
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        query.RequestType.Should().Be(CartRequestTypes.CurrentUser);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PanelUser Tests

    [Fact]
    public void Validate_WhenPanelUserHasValidTableId_ShouldNotHaveValidationError()
    {
        // Arrange
        var query = CreateValidPanelQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RestaurantTableId);
    }

    [Fact]
    public void Validate_WhenPanelUserHasNullTableId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var query = CreateValidPanelQuery() with
        {
            RestaurantTableId = null
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasEmptyTableId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var query = CreateValidPanelQuery() with
        {
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasUserId_ShouldHaveValidationError()
    {
        // Arrange
        var query = CreateValidPanelQuery() with
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenPanelUserHasEmptyUserId_ShouldHaveValidationError()
    {
        // Arrange
        var query = CreateValidPanelQuery() with
        {
            UserId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

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
        var query = CreateValidCurrentUserQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validate_WhenCurrentUserHasNullUserId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var query = CreateValidCurrentUserQuery() with
        {
            UserId = null
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasEmptyUserId_ShouldHaveRequiredValidationError()
    {
        // Arrange
        var query = CreateValidCurrentUserQuery() with
        {
            UserId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasTableId_ShouldHaveValidationError()
    {
        // Arrange
        var query = CreateValidCurrentUserQuery() with
        {
            RestaurantTableId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage)
            .Only();
    }

    [Fact]
    public void Validate_WhenCurrentUserHasEmptyTableId_ShouldHaveValidationError()
    {
        // Arrange
        var query = CreateValidCurrentUserQuery() with
        {
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

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
        var query = CreateValidCurrentUserQuery() with
        {
            RequestType = (CartRequestTypes)requestType
        };

        // Act
        var result = _validator.TestValidate(query);

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
        var query = CreateValidCurrentUserQuery() with
        {
            UserId = null,
            RestaurantTableId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(CurrentUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(CurrentUserTableIdMessage);

        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_WhenPanelUserHasMultipleInvalidProperties_ShouldReturnAllErrors()
    {
        // Arrange
        var query = CreateValidPanelQuery() with
        {
            UserId = Guid.NewGuid(),
            RestaurantTableId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(PanelUserIdMessage);

        result.ShouldHaveValidationErrorFor(x => x.RestaurantTableId)
            .WithErrorMessage(PanelTableIdMessage);

        result.Errors.Should().HaveCount(2);
    }

    #endregion

    #region Conditional Validation Tests

    [Fact]
    public void Validate_WhenPanelUserIsValid_ShouldNotApplyCurrentUserRules()
    {
        // Arrange
        var query = CreateValidPanelQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCurrentUserIsValid_ShouldNotApplyPanelUserRules()
    {
        // Arrange
        var query = CreateValidCurrentUserQuery();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
