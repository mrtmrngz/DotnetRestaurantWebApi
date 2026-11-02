using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Orders.Queries.GetPanelOrderDetailQuery;

namespace RestaurantApi.UnitTests.Features.Orders;

public class GetPanelOrderDetailQueryValidatorTests
{
    private const string OrderIdRequiredMessage = "Sipariş ID bilgisi zorunludur ve boş olamaz.";
    private const string NotEmptyValidatorErrorCode = "NotEmptyValidator";

    private readonly GetPanelOrderDetailQueryValidator _sut = new();

    #region Helper Methods

    private static GetPanelOrderDetailQuery CreateValidQuery()
    {
        return new GetPanelOrderDetailQuery { OrderId = Guid.NewGuid() };
    }

    #endregion

    #region Valid Query Tests

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = _sut.TestValidate(CreateValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f")]
    [InlineData("7b0e6a1c-5d2f-4b8a-9e3c-1a2b3c4d5e6f")]
    public void Validate_WhenOrderIdIsProvided_ShouldNotHaveValidationErrorForOrderId(string orderId)
    {
        var query = new GetPanelOrderDetailQuery { OrderId = Guid.Parse(orderId) };

        var result = _sut.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }

    #endregion

    #region OrderId Required Tests

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveRequiredValidationErrorForOrderId()
    {
        var query = new GetPanelOrderDetailQuery { OrderId = Guid.Empty };

        var result = _sut.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    [Fact]
    public void Validate_WhenQueryIsJustCreated_ShouldHaveRequiredValidationErrorForOrderId()
    {
        // Controller, route'tan gelen sipariş id'sini set etmeyi atlarsa (boş query) sipariş bulunamaz:
        // bu durumda net bir doğrulama hatası dönmelidir.
        var result = _sut.TestValidate(new GetPanelOrderDetailQuery());

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    [Fact]
    public async Task ValidateAsync_WhenOrderIdIsEmpty_ShouldHaveRequiredValidationErrorForOrderId()
    {
        // Eşzamanlı (async) doğrulama yolu da aynı hatayı üretmelidir.
        var result = await _sut.TestValidateAsync(new GetPanelOrderDetailQuery { OrderId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage(OrderIdRequiredMessage);
    }

    #endregion

    #region Error Contract Tests

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldReportSingleErrorWithNotEmptyValidatorCode()
    {
        // Hata sözleşmesi: tek hata, sipariş id alanı, beklenen mesaj, doğrulayıcı kodu ve hata seviyesi.
        var result = _sut.TestValidate(new GetPanelOrderDetailQuery { OrderId = Guid.Empty });

        result.Errors.Should().HaveCount(1, "tek alan doğrulandığı için tek hata dönmelidir.");

        var error = result.Errors.Single();

        error.PropertyName.Should().Be(nameof(GetPanelOrderDetailQuery.OrderId));
        error.ErrorMessage.Should().Be(OrderIdRequiredMessage);
        error.ErrorCode.Should().Be(NotEmptyValidatorErrorCode,
            "istemci hata koduna göre davranabildiği için doğrulayıcı kodu sözleşmenin parçasıdır.");
        error.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldNotThrow()
    {
        // Doğrulama, hata fırlatmak yerine alan bazında raporlar (fırlatmayı pipeline üstlenir).
        Action act = () => _sut.TestValidate(new GetPanelOrderDetailQuery { OrderId = Guid.Empty });

        act.Should().NotThrow("Doğrulama, eksik alanı hata olarak raporlamalı ve istisna fırlatmamalıdır");
    }

    #endregion
}
