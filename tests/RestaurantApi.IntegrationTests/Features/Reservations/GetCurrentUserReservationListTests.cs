using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class GetCurrentUserReservationListTests: BaseIntegrationTest
{
    private const string Route = "/api/reservations/public";
    
    public GetCurrentUserReservationListTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task GetCurrentUserReservationList_WhenValidRequest_ShouldReturn200()
    {
        var testSetup = await CreateVanillaUserAsync();
        var reservations = await SeedReservationAsync(testSetup.User);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testSetup.AccessToken);

        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Sonuç başarılı olacağı için response durum kodu 200 (OK) olarak dönmeliydi.");

        var responseBody =
            await response
                .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi.");

        responseBody.Data.Should().HaveCount(1,
            "Response body içersindeki rezervasyonların sayısı, kullanıcının rezervasyon sayısı (1) ile aynı olmalıydı.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TEST START
    
    [Fact]
    public async Task GetCurrentUserReservationList_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için durum kodu UNAUTHORIZED (401) olarak dönmeliydi.");
    }
    
    // ERROR TEST END
    
    // SETUP
    private async Task<IReadOnlyList<Reservation>> SeedReservationAsync(AppUser user)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var user2 = await CreateVanillaUserAsync();

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

        Reservation reservation1 = new Reservation
        {
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            StartTime = DateTime.UtcNow.AddDays(-7),
            EndTime = DateTime.UtcNow.AddDays(-5),
            IsDeleted = false,
            RestaurantTableId = table.Id,
            Note = "",
            Email = user.Email!,
            UserId = user.Id,
            FullName = user.GetFullName(),
            GuestCount = 3,
            Id = Guid.NewGuid(),
            PhoneNumber = "+901234567890",
            Status = ReservationStatus.Completed
        };
        
        Reservation reservation2 = new Reservation
        {
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            StartTime = DateTime.UtcNow.AddDays(-4),
            EndTime = DateTime.UtcNow.AddDays(1),
            IsDeleted = false,
            RestaurantTableId = table.Id,
            Note = "",
            Email = user2.User.Email!,
            UserId = user2.User.Id,
            FullName = user2.User.GetFullName(),
            GuestCount = 2,
            Id = Guid.NewGuid(),
            PhoneNumber = "+901234567890",
            Status = ReservationStatus.Confirmed
        };

        IReadOnlyList<Reservation> reservationList = new List<Reservation> { reservation1, reservation2 };

        context.Reservations.AddRange(reservationList);

        await context.SaveChangesAsync();

        return reservationList;
    }
}