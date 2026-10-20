using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.Rules.ReservationRules;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class CreatePanelReservationTests: BaseIntegrationTest
{
    private const string Endpoint = "/api/reservations/add/panel";
    
    public CreatePanelReservationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
     [Fact]
    public async Task CreatePanelReservation_WhenRequestIsValidForToday_ShouldReturn201()
    {
        using var clock = FreezeLocalNow(15);

        var table = await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var startLocal = LocalToday.AddHours(16);
        var endLocal = LocalToday.AddHours(17);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
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

        reservation.UserId.Should().BeNull("Rezervasyonu panel oluşturduğu için user id null gelmeliydi.");
        reservation.RestaurantTableId.Should().Be(table.Id);
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.FullName.Should().Be("Jane Doe");
        reservation.Email.Should().Be("jane@gmail.com");
        reservation.GuestCount.Should().Be(4);
        reservation.PhoneNumber.Should().Be("+905441234567");
        reservation.Note.Should().Be("Pencere kenarı");
        reservation.StartTime.Should().Be(startLocal - ReservationSchedule.LocalOffset,
            "Yerel başlangıç saati UTC karşılığı olarak saklanmalıydı.");
        reservation.EndTime.Should().Be(endLocal - ReservationSchedule.LocalOffset);
    }

    [Fact]
    public async Task CreatePanelReservation_WhenTodayIsBookedForSameHours_ShouldReturn201ForTomorrow()
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
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
            startTime = startLocal,
            endTime = endLocal,
            tableId = table.Id,
            note = "Pencere kenarı"
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
    public async Task CreatePanelReservation_WhenStartTimeIsBeforeEarliestStartHour_ShouldReturn400()
    {
        using var clock = FreezeLocalNow(15);

        await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
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
    public async Task CreatePanelReservation_WhenUserUnauthorized_ShouldReturn401()
    {
        using var clock = FreezeLocalNow(15);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task CreatePanelReservation_WhenUserHasNoCreatePermission_ShouldReturn403()
    {
        using var clock = FreezeLocalNow(15);

        await SeedTableAsync(capacity: 6, isActive: true);

        var setupResult = await CreateVanillaUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının reservation:create yetkisi olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task CreatePanelReservation_WhenTableIsPassive_ShouldReturn409()
    {
        using var clock = FreezeLocalNow(15);

        var passiveTable = await SeedTableAsync(capacity: 6, isActive: false);

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PostAsJsonAsync(Endpoint, new
        {
            guestCount = 4,
            fullName = "Jane Doe",
            email = "jane@gmail.com",
            phoneNumber = "+905441234567",
            startTime = LocalToday.AddHours(16),
            endTime = LocalToday.AddHours(17),
            tableId = passiveTable.Id
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Panel rezervasyon oluşturma akışında pasif masa, CONFLICT 409 cevabı ile dönmeliydi.");

        var body = await response.ReadContentAsAsync<ErrorResponse>();
        body!.Message.Should().Be("Pasif masa için rezervasyon oluşturulamaz.",
            "Masanın pasif olduğu bilgisi panel kullanıcısına belirtilmeliydi.");
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
    
    private async Task SeedReservationAsync(Guid tableId, DateTime startLocal, DateTime endLocal)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.Reservations.Add(new Domain.Entities.Reservation
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            GuestCount = 2,
            Status = ReservationStatus.Confirmed,
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