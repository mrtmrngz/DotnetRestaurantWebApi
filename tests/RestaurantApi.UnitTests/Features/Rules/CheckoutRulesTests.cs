using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CheckoutRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class CheckoutRulesTests
{
    private readonly ILogger<CheckoutRules> _loggerMock = Substitute.For<ILogger<CheckoutRules>>();
    private readonly CheckoutRules _sut;

    public CheckoutRulesTests()
    {
        _sut = new CheckoutRules(_loggerMock);
    }

    #region ShouldIdempotencyKeyNotExist Tests

    [Fact]
    public void ShouldIdempotencyKeyNotExist_WhenIdempotencyKeyAlreadyExists_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldIdempotencyKeyNotExist(true);

        act.Should().Throw<ConflictException>(
                "Aynı idempotency key ile ikinci kez ödeme başlatılamayacağı için bu hata fırlatılması gerekirdi")
            .WithMessage("Bu sipariş işlemi daha zaten başlatılmış.");
    }

    [Fact]
    public void ShouldIdempotencyKeyNotExist_WhenIdempotencyKeyNotExists_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldIdempotencyKeyNotExist(false);

        act.Should().NotThrow("Idempotency key daha önce kullanılmadığı için hata fırlatılmamalıydı");
    }

    #endregion
}
