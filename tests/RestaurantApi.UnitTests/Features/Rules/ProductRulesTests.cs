using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Products.Queries.AdminProductDetailQuery;
using RestaurantApi.Application.Features.Products.Queries.PublicProductDetailQuery;
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

    #region ShouldPublicProductDetailExist Tests

    [Fact]
    public async Task ShouldPublicProductDetailExist_WhenResultNotExist_ShouldThrowNotFoundException()
    {
        PublicProductDetailQueryResult? result = null;

        Action act = () => _sut.ShouldPublicProductDetailExist(result, "izgara-tavuk");

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldPublicProductDetailExist_WhenResultExist_ShouldNotThrowAnything()
    {
        PublicProductDetailQueryResult? result = new PublicProductDetailQueryResult();

        Action act = () => _sut.ShouldPublicProductDetailExist(result, "izgara-tavuk");

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldAdminProductDetailExist Tests

    [Fact]
    public async Task ShouldAdminProductDetailExist_WhenResultNotExist_ShouldThrowNotFoundException()
    {
        AdminProductDetailQueryResult? result = null;

        Action act = () => _sut.ShouldAdminProductDetailExist(result, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldAdminProductDetailExist_WhenResultExist_ShouldNotThrowAnything()
    {
        AdminProductDetailQueryResult? result = new AdminProductDetailQueryResult();

        Action act = () => _sut.ShouldAdminProductDetailExist(result, Guid.NewGuid());

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldBeValidForDelete Tests

    [Fact]
    public async Task ShouldBeValidForDelete_WhenProductNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Product? product = null;

        Action act = () => _sut.ShouldBeValidForDelete(product, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldBeValidForDelete_WhenProductAlreadyDeleted_ShouldThrowNotFoundException()
    {
        Domain.Entities.Product? product = new Domain.Entities.Product { IsDeleted = true };

        Action act = () => _sut.ShouldBeValidForDelete(product, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldBeValidForDelete_WhenProductCanBeDeleted_ShouldNotThrowAnything()
    {
        Domain.Entities.Product? product = new Domain.Entities.Product { IsDeleted = false };

        Action act = () => _sut.ShouldBeValidForDelete(product, Guid.NewGuid());

        act.Should().NotThrow();
    }

    #endregion
}
