using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class GetAvailableTablesTests: BaseIntegrationTest
{
    public GetAvailableTablesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetAvailableTables_WhenTableHasNoReservation_ShouldReturnThatTable()
    {
        var now = DateTime.UtcNow;

        var freeTable = await SeedTableAsync(number: 5, capacity: 6);
        var busyTable = await SeedTableAsync(number: 6, capacity: 6);
        await SeedReservationAsync(busyTable.Id, ReservationStatus.Pending, now.AddHours(-1), now.AddHours(1));

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync(BuildUri(guestCount: 4));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Müsait masalar başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().HaveCount(1, "Yalnızca rezervasyonu olmayan masa dönmeliydi.");
        body.Data[0].Id.Should().Be(freeTable.Id);
        body.Data[0].Number.Should().Be(5);
        body.Data[0].Capacity.Should().Be(6, "Kapasite mapper üzerinden dönmeliydi.");
    }

    [Fact]
    public async Task GetAvailableTables_WhenReservationIsExpired_ShouldReturnThatTable()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5, capacity: 6);
        await SeedReservationAsync(table.Id, ReservationStatus.Confirmed, now.AddHours(-4), now.AddHours(-2));

        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync(BuildUri(guestCount: 4));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Müsait masalar başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().HaveCount(1, "Bitiş zamanı geçmiş rezervasyon masa meşgul sayılmadığı için masa dönmeliydi.");
        body.Data[0].Number.Should().Be(5);
    }

    [Fact]
    public async Task GetAvailableTables_WhenNoTableExists_ShouldReturnEmptyArray()
    {
        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync(BuildUri(guestCount: 4));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Müsait masalar başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().BeEmpty("Hiç masa bulunmadığı için boş liste dönmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetAvailableTables_WhenParametersAreMissing_ShouldReturn400()
    {
        var setupResult = await CreateAdminUserAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/tables/available");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Zorunlu alanlar gönderilmediği için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
        body.Errors.Should().Contain(e => e.Message == "Misafir sayısı zorunludur.");
        body.Errors.Should().Contain(e => e.Message == "Başlangıç zamanı zorunludur.");
        body.Errors.Should().Contain(e => e.Message == "Bitiş zamanı zorunludur.");
    }

    [Fact]
    public async Task GetAvailableTables_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync(BuildUri(guestCount: 4));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task GetAvailableTables_WhenUserNotAdmin_ShouldReturn403()
    {
        await SeedTableAsync(number: 5, capacity: 6);
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync(BuildUri(guestCount: 4));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private static string BuildUri(int guestCount)
    {
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(3);

        return $"/api/tables/available?guest-count={guestCount}" +
               $"&start-time={Uri.EscapeDataString(startTime.ToString("o"))}" +
               $"&end-time={Uri.EscapeDataString(endTime.ToString("o"))}";
    }

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(int number, int capacity, bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = number,
            Capacity = capacity,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    private async Task SeedReservationAsync(Guid tableId, string status, DateTime startTime, DateTime endTime)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            GuestCount = 4,
            Status = status,
            FullName = "Test User",
            PhoneNumber = "5551112233",
            Email = "test@test.com",
            Note = "test",
            StartTime = startTime,
            EndTime = endTime,
            IsDeleted = false
        });

        await context.SaveChangesAsync();
    }
}
