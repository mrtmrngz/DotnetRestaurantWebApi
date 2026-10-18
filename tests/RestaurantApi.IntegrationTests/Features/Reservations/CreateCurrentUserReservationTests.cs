using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class CreateCurrentUserReservationTests: BaseIntegrationTest
{
    private const string Endpoint = "/api/reservations/add/current-user";

    public CreateCurrentUserReservationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateCurrentUserReservation_WhenRequestIsValidForToday_ShouldReturn201()
    {
        using var clock = FreezeLocalNow(15);

        var table = await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        // yerel saat 15:00 iken 16:00 - 17:00 için rezervasyon (şuandan en az 1 saat sonrası)
        var startLocal = LocalToday.AddHours(16);
        var endLocal = LocalToday.AddHours(17);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = startLocal,
            endTime = endLocal,
            tableId = table.Id,
            note = "Pencere kenarı"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Masa aktif, kapasitesi yeterli ve istenen aralık boş olduğu için rezervasyon oluşturulmalıydı.");

        var body = await response.ReadContentAsAsync<BaseResponse>();
        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS);

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var reservation = await context.Reservations.SingleAsync();

        reservation.UserId.Should().Be(setupResult.User.Id);
        reservation.RestaurantTableId.Should().Be(table.Id);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        reservation.FullName.Should().Be("Admin User");
        reservation.Email.Should().Be(setupResult.User.Email);
        reservation.GuestCount.Should().Be(4);
        reservation.Note.Should().Be("Pencere kenarı");
        reservation.StartTime.Should().Be(startLocal - ReservationSchedule.LocalOffset,
            "Yerel başlangıç saati UTC karşılığı olarak saklanmalıydı.");
        reservation.EndTime.Should().Be(endLocal - ReservationSchedule.LocalOffset);
    }

    [Fact]
    public async Task CreateCurrentUserReservation_WhenTodayIsBookedForSameHours_ShouldReturn201ForTomorrow()
    {
        using var clock = FreezeLocalNow(15);

        var table = await SeedTableAsync(capacity: 6, isActive: true);

        // bugün 16:00 - 17:00 arası masa dolu
        await SeedReservationAsync(table.Id, LocalToday.AddHours(16), LocalToday.AddHours(17));

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        // yarın aynı saatler
        var startLocal = LocalToday.AddDays(1).AddHours(16);
        var endLocal = LocalToday.AddDays(1).AddHours(17);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = startLocal,
            endTime = endLocal,
            tableId = table.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Farklı güne ait rezervasyonlar çakışmadığı için yarın için rezervasyon oluşturulabilmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        (await context.Reservations.CountAsync()).Should().Be(2, "Bugünkü ve yarınki rezervasyon birlikte bulunmalıydı.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateCurrentUserReservation_WhenStartTimeIsBeforeEarliestStartHour_ShouldReturn400()
    {
        using var clock = FreezeLocalNow(15);

        await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = LocalToday.AddHours(10),
            endTime = LocalToday.AddHours(12),
            tableId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Rezervasyon başlangıcı en erken 11:00 olabildiği için 400 dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();
        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR);
        body.Errors.Should().Contain(e => e.Field == "StartTime" &&
                                          e.Message ==
                                          "Rezervasyon başlangıç zamanı restoran açılışından en az 1 saat sonrası olmalıdır (en erken 11:00).");
    }

    [Fact]
    public async Task CreateCurrentUserReservation_WhenUserUnauthorized_ShouldReturn401()
    {
        using var clock = FreezeLocalNow(15);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task CreateCurrentUserReservation_WhenUserHasNoCreatePermission_ShouldReturn403()
    {
        using var clock = FreezeLocalNow(15);

        await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateVanillaUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının reservation:create yetkisi olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task CreateCurrentUserReservation_WhenUserNotExistInDatabase_ShouldReturn404()
    {
        using var clock = FreezeLocalNow(15);

        var table = await SeedTableAsync(capacity: 6, isActive: true);

        // token geçerli ve ADMIN rolüne sahip fakat kullanıcı veritabanında yok
        var ghostUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "ghost@test.com",
            NormalizedEmail = "GHOST@TEST.COM",
            UserName = "ghost@test.com",
            NormalizedUserName = "GHOST@TEST.COM",
            Name = "Ghost",
            Surname = "User"
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var accessToken = await tokenService.CreateAccessToken(ghostUser, new List<string> { "ADMIN" });

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = table.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Kullanıcı veritabanında bulunmadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<ErrorResponse>();
        body!.Message.Should().Be("Kullanıcı bulunamadı.");
    }

    [Fact]
    public async Task CreateCurrentUserReservation_WhenTableIsPassive_ShouldReturn404WithoutLeakingTableState()
    {
        using var clock = FreezeLocalNow(15);

        var passiveTable = await SeedTableAsync(capacity: 6, isActive: false);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = passiveTable.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "current user akışında pasif masa, 'masa bulunamadı' cevabı ile dönmeliydi.");

        var body = await response.ReadContentAsAsync<ErrorResponse>();
        body!.Message.Should().Be("Masa bulunamadı.",
            "Masanın pasif olduğu bilgisi current user istemcisine sızdırılmamalıdır.");
    }

    // ERROR TESTS END

    // SETUP

    private static DateTime LocalToday => DateTime.UtcNow.Add(ReservationSchedule.LocalOffset).Date;

    private TestTimeProvider TestClock => Factory.Services.GetRequiredService<TestTimeProvider>();

    // Bugünün yerel saatini (UTC+03:00) verilen saate dondurur; test bittiğinde gerçek zamana döner.
    private IDisposable FreezeLocalNow(int hour, int minute = 0)
    {
        var localWallClock = LocalToday.AddHours(hour).AddMinutes(minute);

        TestClock.SetUtcNow(new DateTimeOffset(localWallClock - ReservationSchedule.LocalOffset, TimeSpan.Zero));

        return new TestClockScope(TestClock);
    }

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(int capacity, bool isActive)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = Random.Shared.Next(1, 10_000),
            Capacity = capacity,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    // Yerel saat alınır ve üretimdeki gibi UTC karşılığı olarak kaydedilir.
    private async Task SeedReservationAsync(Guid tableId, DateTime startLocal, DateTime endLocal)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.Reservations.Add(new Domain.Entities.Reservation
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            GuestCount = 2,
            Status = ReservationStatus.Pending,
            FullName = "Test User",
            PhoneNumber = "5551112233",
            Email = "test@test.com",
            Note = "test",
            StartTime = ReservationSchedule.ToUtcTime(startLocal),
            EndTime = ReservationSchedule.ToUtcTime(endLocal),
            IsDeleted = false
        });

        await context.SaveChangesAsync();
    }

    private sealed class TestClockScope: IDisposable
    {
        private readonly TestTimeProvider _timeProvider;

        public TestClockScope(TestTimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public void Dispose()
        {
            _timeProvider.Reset();
        }
    }
}
