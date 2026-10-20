using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class PublicCancelReservationTests : BaseIntegrationTest
{
    private string route = "/api/reservations/public/canceled/";

    public PublicCancelReservationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task PublicCancelReservation_WhenValidData_ShouldCancelReservation()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1), userSetup.User);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        route += res.Id;

        var response = await Client.PatchAsync(route, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Rezervasyon başarılı bir şekilde silinmesi gerekirdi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START
    
    [Fact]
    public async Task PublicCancelReservation_WhenUserNotLoggedIn_ShouldReturn401()
    {
        route += Guid.NewGuid();

        var response = await Client.PatchAsync(route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Kullanıcı giriş yapmadığı için 401 dönmeliydi.");
    }
    
    [Fact]
    public async Task PublicCancelReservation_WhenReservationNotBelongsToCurrentUser_ShouldThrowConflictException()
    {
        var userSetup1 = await CreateVanillaUserAsync();
        var userSetup2 = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1), userSetup1.User);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup2.AccessToken);

        route += res.Id;

        var response = await Client.PatchAsync(route, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Hedef rezervasyon kullanıcıya ait olmadığı için NotFound dönemliydi.");
    }

    [Fact]
    public async Task PublicCancelReservation_WhenReservationAlreadyCanceled_ShouldThrowConflictException()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1), userSetup.User,
            r => r.Status = ReservationStatus.Canceled);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        route += res.Id;

        var response = await Client.PatchAsync(route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "Rezervasyon silindiği için conflict hatası dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP
    private async Task<Domain.Entities.Reservation> SeedReservationAsync(DateTime startTime, DateTime endTime,
        AppUser user, Action<Reservation>? customConfig = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        Domain.Entities.RestaurantTable table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Capacity = 6,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            IsActive = true,
            Number = 1
        };

        context.RestaurantTables.Add(table);
        await context.SaveChangesAsync();

        Reservation reservation = new Reservation
        {
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            StartTime = startTime,
            EndTime = endTime,
            IsDeleted = false,
            RestaurantTableId = table.Id,
            Note = "",
            UserId = user.Id,
            FullName = user.GetFullName(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? "+9005441234567",
            Status = ReservationStatus.Pending
        };

        customConfig?.Invoke(reservation);

        context.Reservations.Add(reservation);

        await context.SaveChangesAsync();

        return reservation;
    }
}