using FluentAssertions;
using FluentValidation.TestHelper;
using RestaurantApi.Application.Features.Reservation.Commands.CreatePanelReservationCommand;
using RestaurantApi.Application.Features.Rules.ReservationRules;

namespace RestaurantApi.UnitTests.Features.Reservations;

public class CreatePanelReservationCommandValidatorTests
{
    private const string FullNameRequiredMessage = "Rezervasyon sahibinin ismi zorunldur.";
    private const string FullNameLengthMessage = "İsim en fazla 100 karakter olabilir.";
    private const string PhoneRequiredMessage = "Telefon numarası boş olamaz";
    private const string PhoneLengthMessage = "Telefon numarası maksimum 15 karakter olmalıdır.";
    private const string EmailRequiredMessage = "Email alanı boş olamaz.";
    private const string EmailInvalidMessage = "Geçersiz email adresi.";
    private const string TableIdRequiredMessage = "Masa seçilmelidir.";
    private const string GuestCountRequiredMessage = "Misafir sayısı zorunludur.";
    private const string GuestCountRangeMessage = "Misafir sayısı 0'dan büyük olmalıdır.";
    private const string NoteLengthMessage = "Not en fazla 350 karakter olabilir.";
    private const string StartTimeRequiredMessage = "Başlangıç zamanı zorunludur.";
    private const string StartTimeEarliestMessage =
        "Rezervasyon başlangıç zamanı restoran açılışından en az 1 saat sonrası olmalıdır (en erken 11:00).";
    private const string StartTimeNowOffsetMessage = "Rezervasyon başlangıç zamanı şuandan en az 1 saat sonrası olmalıdır.";
    private const string StartTimeLatestMessage =
        "Rezervasyon başlangıç zamanı restoran kapanışından en fazla 1.30 saat önce olabilir (en geç 22:30).";
    private const string EndTimeRequiredMessage = "Bitiş zamanı zorunludur.";
    private const string EndTimeAfterStartMessage = "Bitiş zamanı başlangıç zamanından sonra olmalıdır.";
    private const string EndTimeLatestMessage =
        "Rezervasyon bitiş zamanı, başlangıç günü için en geç 23:30 olabilir (kapanıştan en fazla 30 dakika önce).";
    private const string DurationMessage = "Rezervasyon süresi en fazla 3 saat olabilir.";

    private const string ValidFullName = "Ahmet Yılmaz";
    private const string ValidPhone = "05321234567";
    private const string ValidEmail = "ahmet@example.com";

    private static readonly DateTime Today = new(2026, 5, 10);

    #region Helper Methods

    // yerel (UTC+03:00) duvar saati
    private static DateTime Local(int hour, int minute = 0, int second = 0, int dayOffset = 0)
    {
        return Today.AddDays(dayOffset).AddHours(hour).AddMinutes(minute).AddSeconds(second);
    }

    // test saati varsayılan olarak 10 Mayıs yerel 15:00'a dondurulur
    private static CreatePanelReservationCommandValidator CreateValidator(int nowHour = 15, int nowMinute = 0)
    {
        var utcNow = Local(nowHour, nowMinute) - ReservationSchedule.LocalOffset;

        return new CreatePanelReservationCommandValidator(new StubTimeProvider(utcNow));
    }

    private static TestValidationResult<CreatePanelReservationCommand> Validate(DateTime startTime,
        DateTime endTime, string fullName = ValidFullName, string phoneNumber = ValidPhone,
        string email = ValidEmail, int guestCount = 4, Guid? tableId = null, string? note = null,
        int nowHour = 15)
    {
        // NOT: Command constructor parametre isimleri/sırası farklıysa burayı düzelt.
        var command = new CreatePanelReservationCommand(
            FullName: fullName,
            PhoneNumber: phoneNumber,
            Email: email,
            GuestCount: guestCount,
            StartTime: startTime,
            EndTime: endTime,
            TableId: tableId ?? Guid.NewGuid(),
            Note: note);

        return CreateValidator(nowHour: nowHour).TestValidate(command);
    }

    private sealed class StubTimeProvider: TimeProvider
    {
        private readonly DateTime _utcNow;

        public StubTimeProvider(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(_utcNow, TimeSpan.Zero);
        }
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldNotHaveAnyValidationErrors()
    {
        var result = Validate(Local(16), Local(17), guestCount: 4, note: "Pencere kenarı");

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenNoteIsNotProvided_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(16), Local(17), note: null);

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Validate_WhenNoteIsExactlyMaxLength_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(16), Local(17), note: new string('a', 350));

        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void Validate_WhenReservationIsForNextDay_ShouldNotHaveAnyValidationErrors()
    {
        var result = Validate(Local(12, dayOffset: 1), Local(13, dayOffset: 1));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDurationIsExactlyThreeHours_ShouldNotHaveAnyValidationErrors()
    {
        var result = Validate(Local(16), Local(19));

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region FullName Tests

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenFullNameIsEmpty_ShouldHaveRequiredValidationError(string? fullName)
    {
        var result = Validate(Local(16), Local(17), fullName: fullName!);

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage(FullNameRequiredMessage);
    }

    [Fact]
    public void Validate_WhenFullNameIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var result = Validate(Local(16), Local(17), fullName: new string('a', 101));

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage(FullNameLengthMessage);
    }

    [Fact]
    public void Validate_WhenFullNameIsExactlyMaxLength_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(16), Local(17), fullName: new string('a', 100));

        result.ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    #endregion

    #region PhoneNumber Tests

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenPhoneNumberIsEmpty_ShouldHaveRequiredValidationError(string? phoneNumber)
    {
        var result = Validate(Local(16), Local(17), phoneNumber: phoneNumber!);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage(PhoneRequiredMessage);
    }

    [Fact]
    public void Validate_WhenPhoneNumberIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var result = Validate(Local(16), Local(17), phoneNumber: new string('5', 16));

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage(PhoneLengthMessage);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefghijk")]
    [InlineData("0212123")]
    public void Validate_WhenPhoneNumberIsNotValidTurkishNumber_ShouldHaveValidationError(string phoneNumber)
    {
        var result = Validate(Local(16), Local(17), phoneNumber: phoneNumber);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    // BeValidTurkishNumber'ın kabul ettiği formatlara göre bu listeyi düzenle
    [Theory]
    [InlineData("05321234567")]
    [InlineData("5321234567")]
    [InlineData("+905321234567")]
    public void Validate_WhenPhoneNumberIsValidTurkishNumber_ShouldNotHaveValidationError(string phoneNumber)
    {
        var result = Validate(Local(16), Local(17), phoneNumber: phoneNumber);

        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region Email Tests

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenEmailIsEmpty_ShouldHaveRequiredValidationError(string? email)
    {
        var result = Validate(Local(16), Local(17), email: email!);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(EmailRequiredMessage);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc@")]
    [InlineData("@example.com")]
    [InlineData("abc@@example.com")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveInvalidEmailValidationError(string email)
    {
        var result = Validate(Local(16), Local(17), email: email);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage(EmailInvalidMessage);
    }

    [Fact]
    public void Validate_WhenEmailIsValid_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(16), Local(17), email: "test.user@example.com");

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region TableId Tests

    [Fact]
    public void Validate_WhenTableIdIsEmpty_ShouldHaveRequiredValidationError()
    {
        var result = Validate(Local(16), Local(17), tableId: Guid.Empty);

        result.ShouldHaveValidationErrorFor(x => x.TableId)
            .WithErrorMessage(TableIdRequiredMessage);
    }

    #endregion

    #region GuestCount Tests

    [Fact]
    public void Validate_WhenGuestCountIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = Validate(Local(16), Local(17), guestCount: 0);

        result.ShouldHaveValidationErrorFor(x => x.GuestCount)
            .WithErrorMessage(GuestCountRequiredMessage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WhenGuestCountIsNegative_ShouldHaveRangeValidationError(int guestCount)
    {
        var result = Validate(Local(16), Local(17), guestCount: guestCount);

        result.ShouldHaveValidationErrorFor(x => x.GuestCount)
            .WithErrorMessage(GuestCountRangeMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(20)]
    public void Validate_WhenGuestCountIsPositive_ShouldNotHaveValidationError(int guestCount)
    {
        var result = Validate(Local(16), Local(17), guestCount: guestCount);

        result.ShouldNotHaveValidationErrorFor(x => x.GuestCount);
    }

    #endregion

    #region Note Tests

    [Fact]
    public void Validate_WhenNoteIsLongerThanMaxLength_ShouldHaveLengthValidationError()
    {
        var result = Validate(Local(16), Local(17), note: new string('a', 351));

        result.ShouldHaveValidationErrorFor(x => x.Note)
            .WithErrorMessage(NoteLengthMessage);
    }

    #endregion

    #region StartTime Tests

    [Fact]
    public void Validate_WhenStartTimeIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = Validate(default, Local(17));

        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage(StartTimeRequiredMessage);
    }

    [Fact]
    public void Validate_WhenStartTimeIsMissing_ShouldNotHaveOtherDateErrors()
    {
        var result = Validate(default, Local(17));

        result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
        result.Errors.Should().HaveCount(1, "Yalnızca zorunlu alan hatası dönmeliydi.");
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(10, 59)]
    public void Validate_WhenStartTimeIsBeforeEarliestStartHour_ShouldHaveEarliestStartValidationError(int hour,
        int minute)
    {
        var result = Validate(Local(hour, minute), Local(12));

        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage(StartTimeEarliestMessage);
    }

    [Fact]
    public void Validate_WhenStartTimeIsExactlyEarliestStartHour_ShouldNotHaveValidationError()
    {
        // yerel saat 08:00 iken 11:00 için rezervasyon alınabilir
        var result = Validate(Local(11), Local(12), nowHour: 8);

        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validate_WhenStartTimeIsEarlierThanOneHourFromNow_ShouldHaveNowOffsetValidationError()
    {
        var result = Validate(Local(15, 59), Local(17));

        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage(StartTimeNowOffsetMessage);
    }

    [Fact]
    public void Validate_WhenStartTimeIsExactlyOneHourFromNow_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(16), Local(17));

        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validate_WhenStartTimeIsExactlyLatestStartHour_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(22, 30), Local(23, 30));

        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

    [Fact]
    public void Validate_WhenStartTimeIsAfterLatestStartHour_ShouldHaveLatestStartValidationError()
    {
        var result = Validate(Local(22, 31), Local(23, 30));

        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage(StartTimeLatestMessage);
    }

    #endregion

    #region EndTime Tests

    [Fact]
    public void Validate_WhenEndTimeIsMissing_ShouldHaveRequiredValidationError()
    {
        var result = Validate(Local(16), default);

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(EndTimeRequiredMessage);
    }

    [Fact]
    public void Validate_WhenEndTimeEqualsStartTime_ShouldHaveAfterStartValidationError()
    {
        var result = Validate(Local(16), Local(16));

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(EndTimeAfterStartMessage);
    }

    [Fact]
    public void Validate_WhenEndTimeIsBeforeStartTime_ShouldHaveAfterStartValidationError()
    {
        var result = Validate(Local(16), Local(15));

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(EndTimeAfterStartMessage);
    }

    [Fact]
    public void Validate_WhenEndTimeIsExactlyLatestEndHour_ShouldNotHaveValidationError()
    {
        var result = Validate(Local(22), Local(23, 30));

        result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
    }

    [Theory]
    [InlineData(23, 31, 0)]
    [InlineData(23, 30, 1)]
    public void Validate_WhenEndTimeIsAfterLatestEndHour_ShouldHaveLatestEndValidationError(int hour, int minute,
        int second)
    {
        var result = Validate(Local(22), Local(hour, minute, second));

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(EndTimeLatestMessage);
    }

    [Fact]
    public void Validate_WhenEndTimeIsOnNextDay_ShouldHaveLatestEndValidationError()
    {
        // 22:00'da başlayan rezervasyon gece yarısını geçemez (başlangıç günü en geç 23:30)
        var result = Validate(Local(22), Local(1, dayOffset: 1));

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(EndTimeLatestMessage);
    }

    [Fact]
    public void Validate_WhenDurationIsLongerThanThreeHours_ShouldHaveDurationValidationError()
    {
        var result = Validate(Local(16), Local(19, 1));

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage(DurationMessage);
    }

    #endregion
}