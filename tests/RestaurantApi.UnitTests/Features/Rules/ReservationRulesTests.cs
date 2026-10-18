using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.ReservationRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class ReservationRulesTests
{
    private readonly ReservationRules _sut;

    public ReservationRulesTests()
    {
        _sut = new ReservationRules(Substitute.For<ILogger<ReservationRules>>());
    }

    #region ShouldTableBeActiveForReservation Tests

    [Fact]
    public void ShouldTableBeActiveForReservation_WhenTableIsActive_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableBeActiveForReservation(true, Guid.NewGuid(), isPanelRequest: false);

        act.Should().NotThrow("Aktif masa için rezervasyon engellenmemeliydi.");
    }

    [Fact]
    public void ShouldTableBeActiveForReservation_WhenTableIsPassiveAndRequestIsFromPanel_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldTableBeActiveForReservation(false, Guid.NewGuid(), isPanelRequest: true);

        act.Should().Throw<ConflictException>()
            .WithMessage("Pasif masa için rezervasyon oluşturulamaz.");
    }

    [Fact]
    public void ShouldTableBeActiveForReservation_WhenTableIsPassiveAndRequestIsFromCurrentUser_ShouldThrowNotFoundException()
    {
        Action act = () => _sut.ShouldTableBeActiveForReservation(false, Guid.NewGuid(), isPanelRequest: false);

        act.Should().Throw<NotFoundException>()
            .WithMessage("Masa bulunamadı.",
                "current user akışında masanın pasif olduğu bilgisi sızdırılmamalıdır.");
    }

    #endregion

    #region ShouldTableCapacityBeEnough Tests

    [Fact]
    public void ShouldTableCapacityBeEnough_WhenGuestCountEqualsCapacity_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableCapacityBeEnough(6, 6, Guid.NewGuid());

        act.Should().NotThrow("Misafir sayısı kapasiteye eşit olduğunda rezervasyon alınabilir.");
    }

    [Fact]
    public void ShouldTableCapacityBeEnough_WhenGuestCountIsBelowCapacity_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableCapacityBeEnough(6, 4, Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void ShouldTableCapacityBeEnough_WhenGuestCountIsAboveCapacity_ShouldThrowUnprocessableEntityError()
    {
        Action act = () => _sut.ShouldTableCapacityBeEnough(6, 8, Guid.NewGuid());

        act.Should().Throw<UnprocessableEntityError>()
            .WithMessage("*6*8*", "Hata mesajı kapasite ve misafir sayısını içermeliydi.");
    }

    #endregion

    #region ShouldTableNotHaveConflictingReservation Tests

    [Fact]
    public void ShouldTableNotHaveConflictingReservation_WhenNoConflictingReservation_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldTableNotHaveConflictingReservation(false, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3));

        act.Should().NotThrow("Masanın istenen aralıkta çakışan aktif rezervasyonu bulunmuyor.");
    }

    [Fact]
    public void ShouldTableNotHaveConflictingReservation_WhenConflictingReservationExists_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldTableNotHaveConflictingReservation(true, Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(3));

        act.Should().Throw<ConflictException>()
            .WithMessage("Seçilen masa, istenen zaman aralığında dolu.");
    }

    #endregion
}
