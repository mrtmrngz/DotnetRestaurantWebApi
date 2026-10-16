using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.RestaurantTableRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class RestaurantTableRulesTests
{
    private readonly RestaurantTableRules _sut;

    public RestaurantTableRulesTests()
    {
        _sut = new RestaurantTableRules(Substitute.For<ILogger<RestaurantTableRules>>());
    }

    #region ShouldTableNumberBeUnique Tests

    [Fact]
    public async Task ShouldTableNumberBeUnique_WhenTableNumberIsTaken_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldTableNumberBeUnique(true, 5);

        act.Should().Throw<ConflictException>()
            .WithMessage("Bu masa numarası zaten kullanılmaktadır.");
    }

    [Fact]
    public async Task ShouldTableNumberBeUnique_WhenTableNumberIsFree_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableNumberBeUnique(false, 5);

        act.Should().NotThrow();
    }

    #endregion

    #region ShouldTableBeValidForDeletion Tests

    [Fact]
    public async Task ShouldTableBeValidForDeletion_WhenTableNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.RestaurantTable? table = null;

        Action act = () => _sut.ShouldTableBeValidForDeletion(table);

        act.Should().Throw<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    }

    [Fact]
    public async Task ShouldTableBeValidForDeletion_WhenTableIsPassive_ShouldThrowNotFoundException()
    {
        Domain.Entities.RestaurantTable? table = new Domain.Entities.RestaurantTable { IsActive = false };

        Action act = () => _sut.ShouldTableBeValidForDeletion(table);

        act.Should().Throw<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    }

    [Fact]
    public async Task ShouldTableBeValidForDeletion_WhenTableIsActive_ShouldNotThrowAnything()
    {
        Domain.Entities.RestaurantTable? table = new Domain.Entities.RestaurantTable { IsActive = true };

        Action act = () => _sut.ShouldTableBeValidForDeletion(table);

        act.Should().NotThrow();
    }

    #endregion
}
