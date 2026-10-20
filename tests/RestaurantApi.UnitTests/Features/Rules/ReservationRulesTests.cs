using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Domain.Entities;

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
    public void
        ShouldTableBeActiveForReservation_WhenTableIsPassiveAndRequestIsFromCurrentUser_ShouldThrowNotFoundException()
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

    #region ShouldUserIdExistOnCurrentUserReservationCancel Tests

    [Fact]
    public async Task ShouldUserIdExistOnCurrentUserReservationCancel_WhenUserIdNull_ShouldThrowBadRequestException()
    {
        Action act = () => _sut.ShouldUserIdExistOnCurrentUserReservationCancel(null, Guid.NewGuid());

        act.Should().Throw<BadRequestException>().WithMessage("Bu işlemi şuanda gerçekleştiremiyoruz.");
    }

    [Fact]
    public async Task
        ShouldUserIdExistOnCurrentUserReservationCancel_WhenUserIdNotNull_ShouldNotThrowBadRequestException()
    {
        Action act = () => _sut.ShouldUserIdExistOnCurrentUserReservationCancel(Guid.NewGuid(), Guid.NewGuid());

        act.Should().NotThrow("User id olduğu için bir hata fırlatmaması gerekirdi.");
    }

    #endregion

    #region ShouldReservationExist Tests

    [Fact]
    public async Task ShouldReservationExist_WhenReservationNotExist_ShouldThrowNotFoundException()
    {
        Action act = () => _sut.ShouldReservationExist(null, Guid.NewGuid());

        act.Should().Throw<NotFoundException>().WithMessage("Rezervasyon bulunamadı.");
    }

    [Fact]
    public async Task ShouldReservationExist_WhenReservationExist_ShouldNotThrowNotFoundException()
    {
        Action act = () => _sut.ShouldReservationExist(new Domain.Entities.Reservation { }, Guid.NewGuid());

        act.Should().NotThrow("Rezervasyon bulunduğu için herahngi bir hata fırlatmaması gerekirdi.");
    }

    #endregion

    #region ShouldReservationNotCanceledOrCompleted Tests

    [Fact]
    public async Task
        ShouldReservationNotCanceledOrCompleted_WhenReservationStatusCanceled_ShouldThrowConflictException()
    {
        Action act = () => _sut.ShouldReservationNotCanceledOrCompleted(ReservationStatus.Canceled, Guid.NewGuid(),
            CancelReservationTypes.CurrentUser);

        act.Should().Throw<ConflictException>().WithMessage("Rezervasyon zaten iptal edilmiş.");
    }

    [Fact]
    public async Task
        ShouldReservationNotCanceledOrCompleted_WhenReservationStatusCompleted_ShouldThrowUnprocessableException()
    {
        Action act = () => _sut.ShouldReservationNotCanceledOrCompleted(ReservationStatus.Completed, Guid.NewGuid(),
            CancelReservationTypes.CurrentUser);

        act.Should().Throw<UnprocessableEntityError>().WithMessage("Rezervasyon tamamlandığı için iptal edilemez.");
    }

    [Fact]
    public async Task
        ShouldReservationNotCanceledOrCompleted_WhenReservationStatusCompletedButRequestFromAdmin_ShouldNotThrowAnything()
    {
        Action act = () => _sut.ShouldReservationNotCanceledOrCompleted(ReservationStatus.Canceled, Guid.NewGuid(),
            CancelReservationTypes.Admin);

        act.Should().NotThrow("Rezervasyon iptal edilmiş fakat istek admin tarafından yapıldığı için herhangi bir hata fırlatmamalıydı.");
    }

    [Fact]
    public async Task
        ShouldReservationNotCanceledOrCompleted_WhenReservationStatusNotCompletedOrCanceled_ShouldNotThrowAnything()
    {
        Action act = () =>
            _sut.ShouldReservationNotCanceledOrCompleted(ReservationStatus.Pending, Guid.NewGuid(), CancelReservationTypes.CurrentUser);

        act.Should().NotThrow("Rezervasyon iptail edilmemiş veya tamamamlanmamış olmadığı için hata fırlatmamalıydı.");
    }

    #endregion
    
    #region ShouldReservationEndTimeNotPassing Tests

    [Fact]
    public async Task
        ShouldReservationEndTimeNotPassing_WhenEndTimeHigherThanNow_ShouldThrowUnprocessableException()
    {
        Action act = () => _sut.ShouldReservationEndTimeNotPassing(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, Guid.NewGuid());

        act.Should().Throw<UnprocessableEntityError>().WithMessage("Rezervasyon bitiş tarihi zaten geçmiş.");
    }
    
    [Fact]
    public async Task
        ShouldReservationEndTimeNotPassing_WhenEndTimeLowerThanNow_ShouldNotThrowAnything()
    {
        Action act = () =>
            _sut.ShouldReservationEndTimeNotPassing(DateTime.UtcNow.AddDays(1), DateTime.UtcNow, Guid.NewGuid());

        act.Should().NotThrow("Rezervasyonun bitmesine daha zaman olduğu için hata göndermemeliydi.");
    }

    #endregion
}