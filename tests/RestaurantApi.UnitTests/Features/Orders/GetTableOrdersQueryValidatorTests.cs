using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Queries.GetTableOrdersQuery;

namespace RestaurantApi.UnitTests.Features.Orders;

public class GetTableOrdersQueryValidatorTests
{
    private const string TableIdRequiredMessage = "Masa ID bilgisi zorunludur ve boş olamaz.";

    private readonly GetTableOrdersQueryValidator _sut = new();

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenTableIdIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var query = new GetTableOrdersQuery { TableId = Guid.NewGuid() };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TableId Required Tests

    [Fact]
    public void Validate_WhenTableIdIsEmpty_ShouldHaveTableIdRequiredError()
    {
        var query = new GetTableOrdersQuery { TableId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.TableId)
            .WithErrorMessage(TableIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenQueryIsJustCreated_ShouldHaveSingleTableIdRequiredError()
    {
        // Controller, route'tan gelen masa id'sini set etmeyi atlarsa (boş query) tek ve net hata döner.
        var result = _sut.TestValidate(new GetTableOrdersQuery());

        result.ShouldHaveValidationErrorFor(x => x.TableId)
            .WithErrorMessage(TableIdRequiredMessage);

        result.Errors.Should().HaveCount(1, "tek alan doğrulandığı için tek hata dönmelidir.");
    }

    [Fact]
    public void Validate_WhenTableIdIsEmpty_ShouldNotThrow()
    {
        // Doğrulama, hata fırlatmak yerine alan bazında raporlar (fırlatmayı pipeline üstlenir).
        Action act = () => _sut.TestValidate(new GetTableOrdersQuery { TableId = Guid.Empty });

        act.Should().NotThrow("Doğrulama, eksik alanı hata olarak raporlamalı ve istisna fırlatmamalıdır");
    }

    #endregion
}
