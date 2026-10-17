using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.RestaurantTable.Commands.UpdateRestaurantTableCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class UpdateRestaurantTableTests: BaseIntegrationTest
{
    public UpdateRestaurantTableTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task UpdateRestaurantTable_WhenTableHasNoReservation_ShouldReturn200AndClearCache()
    {
        var table = await SeedTableAsync(number: 5, capacity: 4);
        await SeedCachesAsync();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Number: 6, Capacity: 8)));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa başarılı bir şekilde güncellenmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Güncelleme işlemi sonucunda CONTENT_UPDATED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedTable = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id);

        updatedTable.Should().NotBeNull("Masa veritabanında bulunmalıydı.");
        updatedTable!.Number.Should().Be(6);
        updatedTable.Capacity.Should().Be(8);

        await AssertCacheClearedAsync(cache);
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenReservationsAreExpired_ShouldReturn200AndClearCache()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5, capacity: 8);
        await SeedReservationAsync(table.Id, guestCount: 6, ReservationStatus.Pending,
            startTime: now.AddHours(-3), endTime: now.AddHours(-1));
        await SeedReservationAsync(table.Id, guestCount: 4, ReservationStatus.Confirmed,
            startTime: now.AddHours(-5), endTime: now.AddHours(-2));
        await SeedCachesAsync();

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 5)));

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Bitiş zamanı geçmiş rezervasyonlar aktif sayılmadığı için güncelleme yapılmalıydı.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedTable = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id);

        updatedTable!.Capacity.Should().Be(5, "Süresi geçmiş rezervasyonlar kapasite güncellemesini engellememeliydi.");

        await AssertCacheClearedAsync(cache);
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenCapacityCoversActiveReservation_ShouldReturn200AndClearCache()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5, capacity: 4);
        await SeedReservationAsync(table.Id, guestCount: 6, ReservationStatus.Confirmed,
            startTime: now.AddHours(-1), endTime: now.AddHours(1));
        await SeedCachesAsync();

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 6)));

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Yeni kapasite aktif rezervasyonun misafir sayısına eşit olduğu için güncelleme yapılmalıydı.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var updatedTable = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id);

        updatedTable!.Capacity.Should().Be(6);

        await AssertCacheClearedAsync(cache);
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task UpdateRestaurantTable_WhenNoFieldProvided_ShouldReturn400()
    {
        var table = await SeedTableAsync(number: 5, capacity: 4);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Güncellenecek alan gönderilmediği için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
        body.Errors.Should().Contain(e => e.Message == "Güncellenecek en az bir alan belirtilmelidir.",
            "Güncellenecek alan olmadığı için ilgili validasyon hatası dönmeliydi.");
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.PatchAsync($"/api/tables/{Guid.NewGuid()}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 5)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenUserNotAdmin_ShouldReturn403()
    {
        var table = await SeedTableAsync(number: 5, capacity: 4);
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 5)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenTableNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{Guid.NewGuid()}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 5)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Masa bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenDeactivatingTableWithActiveReservation_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5, capacity: 8);
        await SeedReservationAsync(table.Id, guestCount: 2, ReservationStatus.Confirmed,
            startTime: now.AddHours(-1), endTime: now.AddHours(1));

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(IsActive: false)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Aktif rezervasyonu bulunan masa pasife alınamayacağı için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task UpdateRestaurantTable_WhenCapacityIsBelowActiveReservation_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5, capacity: 8);
        await SeedReservationAsync(table.Id, guestCount: 6, ReservationStatus.Pending,
            startTime: now.AddHours(-1), endTime: now.AddHours(1));

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.PatchAsync($"/api/tables/{table.Id}",
            JsonContent.Create(new UpdateRestaurantTableCommand(Capacity: 5)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Yeni kapasite aktif rezervasyonun misafir sayısının altında kaldığı için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
        body.Message.Should().Contain("6", "Hata mesajı aktif rezervasyondaki en yüksek misafir sayısını içermeliydi.");
    }

    // ERROR TESTS END

    // SETUP

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

    private async Task SeedReservationAsync(Guid tableId, int guestCount, string status, DateTime startTime,
        DateTime endTime)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            GuestCount = guestCount,
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

    private async Task SeedCachesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.AdminRestaurantTables(), "tables", TimeSpan.FromMinutes(30));
    }

    private static async Task AssertCacheClearedAsync(ICacheService cache)
    {
        (await cache.GetAsync<string>(CacheKeys.AdminRestaurantTables())).Should().BeNull(
            "Masa güncellendiği için admin masa listesi redisten silinmeliydi.");
    }
}
