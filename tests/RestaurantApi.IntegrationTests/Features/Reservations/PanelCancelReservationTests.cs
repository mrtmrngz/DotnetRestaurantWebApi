using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class PanelCancelReservationTests: BaseIntegrationTest
{
    public string Route = "/api/reservations/panel/canceled/";
    
    public PanelCancelReservationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START
    
    [Fact]
    public async Task PublicCancelReservation_WhenValidData_ShouldCancelReservation()
    {
        var userSetup = await CreateAdminUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1));

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        Route += res.Id;

        var response = await Client.PatchAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Rezervasyon başarılı bir şekilde silinmesi gerekirdi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var reserv = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

            reserv.Should().NotBeNull();
            reserv.Status.Should().Be(ReservationStatus.Canceled, "Rezervasyon iptal edilmeliydi.");
        }
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task PublicCancelReservation_WhenUserNotLoggedIn_ShouldReturn401()
    {
        Route += Guid.NewGuid();

        var response = await Client.PatchAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
            "Kullanıcı giriş yapmadığı için Unauthorized (401) hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task PublicCancelReservation_WhenUserHasNotPermissionForThisRoute_ShouldReturnForbidden()
    {
        var userSetup = await CreateVanillaUserAsync();
        Route += Guid.NewGuid();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        var response = await Client.PatchAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "Kullanıcının bu işlem için yetkisi yoksa (reservation:cancel) Forbidden (403) hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task PublicCancelReservation_WhenReservationEndTimeAlreadyPassed_ShouldCancelReservation()
    {
        var userSetup = await CreateAdminUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        Route += res.Id;

        var response = await Client.PatchAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, 
            "Rezervasyon son bulmaz zamanı bittiği için rezervasyon Unprocessable Entity (422) durum kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP
    private async Task<Domain.Entities.Reservation> SeedReservationAsync(DateTime startTime, DateTime endTime,
        Action<Reservation>? customConfig = null)
    {
        var vanillaUser = await CreateVanillaUserAsync();
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
            UserId = vanillaUser.User.Id,
            FullName = vanillaUser.User.GetFullName(),
            Email = vanillaUser.User.Email!,
            PhoneNumber = "+9005441234567",
            Status = ReservationStatus.Confirmed
        };

        customConfig?.Invoke(reservation);

        context.Reservations.Add(reservation);

        await context.SaveChangesAsync();

        return reservation;
    }
}