using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrderDetailQuery;

namespace RestaurantApi.UnitTests.Features.Orders;

public class GetUserOrderDetailQueryValidatorTests
{
    private const string OrderIdRequiredMessage = "Sipariş ID bilgisi zorunludur ve boş olamaz.";
    private const string UserIdRequiredMessage = "Kullanıcı ID bilgisi zorunludur ve boş olamaz.";

    private readonly GetUserOrderDetailQueryValidator _sut = new();

    #region Helper Methods

    private static GetUserOrderDetailQuery CreateValidQuery()
    {
        return new GetUserOrderDetailQuery
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenQueryIsJustCreated_ShouldNotThrow()
    {
        // Controller route'tan sipariş, token'dan kullanıcı id'sini set etmezse (boş query) doğrulama çökmemeli;
        // hatalar alan bazında raporlanır, istisnayı pipeline fırlatır.
        Action act = () => _sut.TestValidate(new GetUserOrderDetailQuery());

        act.Should().NotThrow("Doğrulama, eksik alanları hata olarak raporlamalı ve istisna fırlatmamalıdır");
    }

    #endregion

    #region Field Required Tests

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveOrderIdRequiredError()
    {
        var query = CreateValidQuery() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var query = CreateValidQuery() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    #endregion

    #region Error Isolation and Multiple Error Tests

    [Fact]
    public void Validate_WhenOnlyOrderIdIsEmpty_ShouldNotHaveUserIdError()
    {
        var query = CreateValidQuery() with { OrderId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.Errors.Should().HaveCount(1, "yalnızca sipariş ID hatası dönmelidir.");
    }

    [Fact]
    public void Validate_WhenOnlyUserIdIsEmpty_ShouldNotHaveOrderIdError()
    {
        var query = CreateValidQuery() with { UserId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
        result.Errors.Should().HaveCount(1, "yalnızca kullanıcı ID hatası dönmelidir.");
    }

    [Fact]
    public void Validate_WhenBothFieldsAreEmpty_ShouldHaveTwoValidationErrors()
    {
        // Boş query: hem sipariş hem kullanıcı ID'si eksik olduğu için iki hata birden raporlanır.
        var result = _sut.TestValidate(new GetUserOrderDetailQuery());

        result.ShouldHaveValidationErrorFor(x => x.OrderId).WithErrorMessage(OrderIdRequiredMessage);
        result.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorMessage(UserIdRequiredMessage);

        result.Errors.Should().HaveCount(2, "iki alan da boş olduğu için iki hata dönmelidir.");
    }

    #endregion
}
