using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.CartRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class CartRulesTests
{
    private readonly ILogger<CartRules> _loggerMock = Substitute.For<ILogger<CartRules>>();
    private readonly CartRules _sut;

    public CartRulesTests()
    {
        _sut = new CartRules(_loggerMock);
    }

    #region ShouldCartExist Tests

    [Fact]
    public void ShouldCartExist_WhenCartNotExist_ShouldThrowNotFoundException()
    {
        Action act = () => _sut.ShouldCartExist(null);

        act.Should().Throw<NotFoundException>("Sepet bulunamadığı için bu hata fırlatılması gerekirdi")
            .WithMessage("Sepet bulunamadı.");
    }
    
    [Fact]
    public void ShouldCartExist_WhenCartExist_ShouldNotThrow()
    {
        Action act = () => _sut.ShouldCartExist(new Domain.Entities.Cart{});

        act.Should().NotThrow("Sepet bulunduğu için bu hata fırlatmaması gerekirdi");
    }

    #endregion
}