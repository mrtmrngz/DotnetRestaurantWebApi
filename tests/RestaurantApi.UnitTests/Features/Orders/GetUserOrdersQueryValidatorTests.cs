using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Queries.GetUserOrdersQuery;

namespace RestaurantApi.UnitTests.Features.Orders;

public class GetUserOrdersQueryValidatorTests
{
    private const string UserIdRequiredMessage = "Kullanıcı ID bilgisi zorunludur ve boş olamaz.";

    private readonly GetUserOrdersQueryValidator _sut = new();

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenUserIdIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var query = new GetUserOrdersQuery { UserId = Guid.NewGuid() };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UserId Required Tests

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldHaveUserIdRequiredError()
    {
        var query = new GetUserOrdersQuery { UserId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenQueryIsJustCreated_ShouldHaveSingleUserIdRequiredError()
    {
        // Controller, kullanıcı id'sini set etmeyi atlarsa (boş query) tek ve net hata döner.
        var result = _sut.TestValidate(new GetUserOrdersQuery());

        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage(UserIdRequiredMessage);

        result.Errors.Should().HaveCount(1, "tek alan doğrulandığı için tek hata dönmelidir.");
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldNotThrow()
    {
        // Doğrulama, hata fırlatmak yerine alan bazında raporlar (fırlatmayı pipeline üstlenir).
        Action act = () => _sut.TestValidate(new GetUserOrdersQuery { UserId = Guid.Empty });

        act.Should().NotThrow("Doğrulama, eksik alanı hata olarak raporlamalı ve istisna fırlatmamalıdır");
    }

    #endregion
}
