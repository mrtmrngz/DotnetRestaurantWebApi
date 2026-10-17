using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class DeleteTableTests: BaseIntegrationTest
{
    public DeleteTableTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task DeleteTable_WhenTableExists_ShouldReturn200AndSoftDeleteAndClearCache()
    {
        var table = await SeedTableAsync(number: 5);
        await SeedCachesAsync();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa başarılı bir şekilde silinmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Silme işlemi sonucunda CONTENT_DELETED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var deletedTable = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id);

        deletedTable.Should().NotBeNull("Masa veritabanında bulunmalıydı (soft delete).");
        deletedTable!.IsActive.Should().BeFalse("Masa silindiği için IsActive=false olarak işaretlenmeliydi.");

        (await cache.GetAsync<string>(CacheKeys.AdminRestaurantTables())).Should().BeNull(
            "Masa silindiği için admin masa listesi redisten silinmeliydi.");
    }

    [Fact]
    public async Task DeleteTable_WhenReservationIsExpired_ShouldReturn200AndSoftDelete()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5);
        await SeedReservationAsync(table.Id, ReservationStatus.Confirmed,
            startTime: now.AddHours(-3), endTime: now.AddHours(-1));
        await SeedCachesAsync();

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Bitiş zamanı geçmiş rezervasyon aktif sayılmadığı için masa silinebilmeliydi.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_DELETED_SUCCESS,
            "Silme işlemi sonucunda CONTENT_DELETED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var deletedTable = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Id == table.Id);

        deletedTable.Should().NotBeNull("Masa veritabanında bulunmalıydı (soft delete).");
        deletedTable!.IsActive.Should().BeFalse("Masa silindiği için IsActive=false olarak işaretlenmeliydi.");

        (await cache.GetAsync<string>(CacheKeys.AdminRestaurantTables())).Should().BeNull(
            "Masa silindiği için admin masa listesi redisten silinmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task DeleteTable_WhenTableNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/tables/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Masa bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task DeleteTable_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.DeleteAsync($"/api/tables/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task DeleteTable_WhenUserNotAdmin_ShouldReturn403()
    {
        var table = await SeedTableAsync(number: 5);
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    [Fact]
    public async Task DeleteTable_WhenActiveReservationExists_ShouldReturn409()
    {
        var now = DateTime.UtcNow;

        var table = await SeedTableAsync(number: 5);
        await SeedReservationAsync(table.Id, ReservationStatus.Pending,
            startTime: now.AddHours(-1), endTime: now.AddHours(1));

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.DeleteAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Aktif rezervasyonu bulunan masa silinemeyeceği için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
        body.Message.Should().Be("Aktif rezervasyonu bulunan masa silinemez.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(int number, bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = number,
            Capacity = 4,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }

    private async Task SeedCachesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.AdminRestaurantTables(), "tables", TimeSpan.FromHours(1));
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
