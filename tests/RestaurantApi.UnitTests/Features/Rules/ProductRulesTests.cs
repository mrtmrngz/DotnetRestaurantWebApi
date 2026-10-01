using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.ProductRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class ProductRulesTests
{
    private readonly ILogger<ProductRules> _loggerMock = Substitute.For<ILogger<ProductRules>>();
    private readonly ProductRules _sut;

    public ProductRulesTests()
    {
        _sut = new ProductRules(_loggerMock);
    }

    #region ValidateExistProductEntity Tests

    [Fact]
    public async Task ValidateExistProductEntity_WhenProductNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Product? product = null;

        Action act = () => _sut.ValidateExistProductEntity(product, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ValidateExistProductEntity_WhenProductExist_ShouldNotThrowAnything()
    {
        Domain.Entities.Product? product = new Domain.Entities.Product();

        Action act = () => _sut.ValidateExistProductEntity(product, Guid.NewGuid());

        act.Should().NotThrow();
    }

    #endregion
}
