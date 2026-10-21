using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CartItemRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class CartItemRulesTests
{
    private readonly ILogger<CartItemRules> _loggerMock = Substitute.For<ILogger<CartItemRules>>();
    private readonly CartItemRules _sut;

    public CartItemRulesTests()
    {
        _sut = new CartItemRules(_loggerMock);
    }

    #region CheckMaximumCartValue Tests

    [Fact]
    public void CheckMaximumCartValue_WhenQuantityGreaterThan100_ShouldThrowUnprocessableEntityException()
    {
        Action act = () => _sut.CheckMaximumCartValue(101);

        act.Should().Throw<UnprocessableEntityError>().WithMessage(
            "Bir üründen sepete en fazla 100 adet eklenebilir. Daha fazlası için restoran ile iletişime geçiniz.");
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    public void CheckMaximumCartValue_WhenQuantityIsWithinLimit_ShouldNotThrow(
        int quantity)
    {
        Action act = () => _sut.CheckMaximumCartValue(quantity);

        act.Should().NotThrow();
    }

    #endregion
}