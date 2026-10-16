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
}
