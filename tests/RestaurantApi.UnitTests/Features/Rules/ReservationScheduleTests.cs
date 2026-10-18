using FluentAssertions;
using RestaurantApi.Application.Features.Rules.ReservationRules;

namespace RestaurantApi.UnitTests.Features.Rules;

public class ReservationScheduleTests
{
    private static readonly DateTime Day = new(2026, 5, 10);

    #region Sınır Hesapları Tests

    [Fact]
    public void GetDayOpening_ShouldReturn10_00()
    {
        ReservationSchedule.GetDayOpening(Day).Should().Be(Day.AddHours(10));
    }

    [Fact]
    public void GetDayClosing_ShouldReturnNextDayMidnight()
    {
        ReservationSchedule.GetDayClosing(Day).Should().Be(Day.AddDays(1),
            "Kapanış 24:00 olduğu için ertesi günün 00:00'ı kabul edilir.");
    }

    [Fact]
    public void GetEarliestStartTime_ShouldReturn11_00()
    {
        ReservationSchedule.GetEarliestStartTime(Day).Should().Be(Day.AddHours(11),
            "Rezervasyon başlangıcı açılıştan en az 1 saat sonrası olabilir.");
    }

    [Fact]
    public void GetLatestStartTime_ShouldReturn22_30()
    {
        ReservationSchedule.GetLatestStartTime(Day).Should().Be(Day.AddHours(22).AddMinutes(30),
            "Rezervasyon başlangıcı kapanıştan en fazla 1.30 saat önce olabilir.");
    }

    [Fact]
    public void GetLatestEndTime_ShouldReturn23_30()
    {
        ReservationSchedule.GetLatestEndTime(Day).Should().Be(Day.AddHours(23).AddMinutes(30),
            "Rezervasyon bitişi kapanıştan en fazla 30 dakika önce olabilir.");
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void ToLocalTime_ShouldAddThreeHoursToUtc()
    {
        ReservationSchedule.ToLocalTime(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc))
            .Should().Be(new DateTime(2026, 5, 10, 15, 0, 0),
                "Türkiye yerel saati UTC+03:00'dir.");
    }

    [Fact]
    public void ToUtcTime_ShouldSubtractThreeHoursAndReturnUtcKind()
    {
        var utcTime = ReservationSchedule.ToUtcTime(new DateTime(2026, 5, 10, 22, 28, 0));

        utcTime.Should().Be(new DateTime(2026, 5, 10, 19, 28, 0));
        utcTime.Kind.Should().Be(DateTimeKind.Utc, "Veritabanına yazımda Kind=Utc olmalıdır.");
    }

    [Fact]
    public void ToUtcTime_AndToLocalTime_ShouldBeReverseOfEachOther()
    {
        var localTime = new DateTime(2026, 5, 10, 22, 28, 0);

        ReservationSchedule.ToLocalTime(ReservationSchedule.ToUtcTime(localTime))
            .Should().Be(localTime);
    }

    #endregion
}
