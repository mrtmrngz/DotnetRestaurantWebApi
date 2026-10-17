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

    #region ShouldTableExist Tests

    [Fact]
    public async Task ShouldTableExist_WhenTableNotExist_ShouldThrowNotFoundException()
    {
        Domain.Entities.RestaurantTable? table = null;

        Action act = () => _sut.ShouldTableExist(table, Guid.NewGuid());

        act.Should().Throw<NotFoundException>()
            .WithMessage("Masa bulunamadı.");
    }

    [Fact]
    public async Task ShouldTableExist_WhenTableExists_ShouldNotThrowAnything()
    {
        Domain.Entities.RestaurantTable? table = new Domain.Entities.RestaurantTable { IsActive = false };

        Action act = () => _sut.ShouldTableExist(table, table.Id);

        act.Should().NotThrow("Pasif masa da veritabanında bulunduğu için hata fırlatılmamalıydı.");
    }

    #endregion

    #region ShouldTableBeUpdatableWithReservations Tests

    [Fact]
    public void ShouldTableBeUpdatableWithReservations_WhenNoActiveReservation_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableBeUpdatableWithReservations(false, 0, requestedCapacity: 1,
            requestedIsActive: false);

        act.Should().NotThrow("Aktif rezervasyon bulunmadığı için tüm güncellemeler serbest olmalıydı.");
    }

    [Fact]
    public void ShouldTableBeUpdatableWithReservations_WhenDeactivatingTableWithActiveReservation_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldTableBeUpdatableWithReservations(true, 2, requestedCapacity: null,
            requestedIsActive: false);

        act.Should().Throw<ConflictException>()
            .WithMessage("Aktif rezervasyonu bulunan masa pasife alınamaz.");
    }

    [Fact]
    public void ShouldTableBeUpdatableWithReservations_WhenCapacityIsBelowActiveReservation_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldTableBeUpdatableWithReservations(true, 6, requestedCapacity: 5,
            requestedIsActive: null);

        act.Should().Throw<ConflictException>()
            .WithMessage("*6*");
    }

    [Fact]
    public void ShouldTableBeUpdatableWithReservations_WhenCapacityEqualsActiveReservation_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableBeUpdatableWithReservations(true, 6, requestedCapacity: 6,
            requestedIsActive: null);

        act.Should().NotThrow("Kapasite aktif rezervasyonun misafir sayısına eşit olduğu için güncellenebilir.");
    }

    [Fact]
    public void ShouldTableBeUpdatableWithReservations_WhenCapacityIsAboveActiveReservation_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableBeUpdatableWithReservations(true, 6, requestedCapacity: 8,
            requestedIsActive: true);

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
