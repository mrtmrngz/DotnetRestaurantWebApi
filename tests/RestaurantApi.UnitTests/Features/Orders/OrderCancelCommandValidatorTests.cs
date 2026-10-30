using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Commands.OrderCancelCommand;

namespace RestaurantApi.UnitTests.Features.Orders;

public class OrderCancelCommandValidatorTests
{
    private const string OrderIdRequiredMessage = "Sipariş ID bilgisi zorunludur.";
    private const string UserIdRequiredMessage = "Kullanıcı ID bilgisi zorunludur.";

    private readonly OrderCancelCommandValidator _sut = new();

    #region Helper Methods

    private static OrderCancelCommand CreateValidCommand()
    {
        return new OrderCancelCommand
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
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
    public void Validate_WhenCommandIsJustCreated_ShouldNotThrow()
    {
        // Boş nesne üzerinde doğrulama çökmemeli; hatalar alan bazında raporlanır.
        Action act = () => _sut.TestValidate(new OrderCancelCommand());

        act.Should().NotThrow("Doğrulama, eksik alanları hata olarak raporlamalı ve istisna fırlatmamalıdır");
    }

    #endregion

    #region Field Required Tests

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveOrderIdRequiredError()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    #endregion

    #region Error Isolation and Multiple Error Tests

    [Fact]
    public void Validate_WhenOnlyOrderIdIsEmpty_ShouldNotHaveUserIdError()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.Errors.Should().HaveCount(1, "yalnızca sipariş ID hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenOnlyUserIdIsEmpty_ShouldNotHaveOrderIdError()
    {
        var command = CreateValidCommand() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
        result.Errors.Should().HaveCount(1, "yalnızca kullanıcı ID hatası dönmeliydi.");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreEmpty_ShouldHaveTwoValidationErrors()
    {
        // Controller route'tan orderId, token'dan userId set etmediyse (boş komut) iki hata döner.
        var result = _sut.TestValidate(new OrderCancelCommand());

        result.ShouldHaveValidationErrorFor(x => x.OrderId).WithErrorMessage(OrderIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorMessage(UserIdRequiredMessage);

        result.Errors.Should().HaveCount(2, "iki alan da boş olduğu için iki hata dönmelidir.");
    }

    #endregion
}
