using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.DiscountRules;
using RestaurantApi.Application.Features.Rules.ProductRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class DiscountRulesTests
{
    private readonly ILogger<DiscountRules> _loggerMock = Substitute.For<ILogger<DiscountRules>>();
    private readonly DiscountRules _sut;

    public DiscountRulesTests()
    {
        _sut = new DiscountRules(_loggerMock, new ProductRules(Substitute.For<ILogger<ProductRules>>()));
    }

    #region ShouldProductBeEligibleForDiscount Tests

    [Fact]
    public async Task ShouldProductBeEligibleForDiscount_WhenProductNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.Product? product = null;

        Action act = () => _sut.ShouldProductBeEligibleForDiscount(product, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldProductBeEligibleForDiscount_WhenProductAlreadyDeleted_ShouldThrowNotFoundException()
    {
        Domain.Entities.Product? product = new Domain.Entities.Product { IsDeleted = true };

        Action act = () => _sut.ShouldProductBeEligibleForDiscount(product, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Ürün bulunamadı.");
    }

    [Fact]
    public async Task ShouldProductBeEligibleForDiscount_WhenProductIsActive_ShouldNotThrowAnything()
    {
        Domain.Entities.Product? product = new Domain.Entities.Product { IsDeleted = false };

        Action act = () => _sut.ShouldProductBeEligibleForDiscount(product, Guid.NewGuid());

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldProductNotHaveDiscount Tests

    [Fact]
    public async Task ShouldProductNotHaveDiscount_WhenProductHasDiscount_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldProductNotHaveDiscount(true, Guid.NewGuid());

        act.Should().Throw<ConflictException>()
            .WithMessage("Bu ürünün zaten bir indirimi bulunmaktadır.");
    }

    [Fact]
    public async Task ShouldProductNotHaveDiscount_WhenProductHasNoDiscount_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldProductNotHaveDiscount(false, Guid.NewGuid());

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldDiscountDatesBeValid Tests

    [Fact]
    public async Task ShouldDiscountDatesBeValid_WhenStartDateIsInThePast_ShouldThrowUnprocessableEntityError()
    {
        var now = DateTime.UtcNow;

        Action act = () => _sut.ShouldDiscountDatesBeValid(now.AddDays(-2), now.AddDays(1), now);

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("İndirim başlangıç tarihi bugünden önce olamaz.");
    }

    [Fact]
    public async Task ShouldDiscountDatesBeValid_WhenEndDateIsInThePast_ShouldThrowUnprocessableEntityError()
    {
        var now = DateTime.UtcNow;

        Action act = () => _sut.ShouldDiscountDatesBeValid(now, now.AddDays(-1), now);

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("İndirim bitiş tarihi bugünden önce olamaz.");
    }

    [Fact]
    public async Task ShouldDiscountDatesBeValid_WhenEndDateIsBeforeStartDate_ShouldThrowUnprocessableEntityError()
    {
        var now = DateTime.UtcNow;

        Action act = () => _sut.ShouldDiscountDatesBeValid(now.AddDays(5), now.AddDays(2), now);

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("İndirim bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
    }

    [Fact]
    public async Task ShouldDiscountDatesBeValid_WhenDatesAreValid_ShouldNotThrowAnything()
    {
        var now = DateTime.UtcNow;

        Action act = () => _sut.ShouldDiscountDatesBeValid(now.AddDays(1), now.AddDays(3), now);

        act.Should().NotThrow();
    }

    #endregion

    #region CalculateIsActive Tests

    [Fact]
    public void CalculateIsActive_WhenNowIsWithinRange_ShouldReturnTrue()
    {
        var now = DateTime.UtcNow;

        var result = _sut.CalculateIsActive(now.AddDays(-1), now.AddDays(1), now);

        result.Should().BeTrue("Şu anki zaman indirim aralığında olduğu için aktif olmalıydı.");
    }

    [Fact]
    public void CalculateIsActive_WhenNowIsBeforeStart_ShouldReturnFalse()
    {
        var now = DateTime.UtcNow;

        var result = _sut.CalculateIsActive(now.AddDays(1), now.AddDays(3), now);

        result.Should().BeFalse("İndirim henüz başlamadığı için pasif olmalıydı.");
    }

    [Fact]
    public void CalculateIsActive_WhenNowIsAfterEnd_ShouldReturnFalse()
    {
        var now = DateTime.UtcNow;

        var result = _sut.CalculateIsActive(now.AddDays(-3), now.AddDays(-1), now);

        result.Should().BeFalse("İndirim sona erdiği için pasif olmalıydı.");
    }

    #endregion
}
