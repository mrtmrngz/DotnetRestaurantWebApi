using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class GetPanelReservationListTests: BaseIntegrationTest
{
    private const string Route = "/api/reservations/panel";
    
    public GetPanelReservationListTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TEST START

    [Fact]
    public async Task GetPanelReservationList_WhenValidRequest_ShouldReturn200()
    {
        var adminSetup = await CreateAdminUserAsync();
        var reservations = await SeedReservationAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);

        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Sonuç başarılı olacağı için response durum kodu 200 (OK) olarak dönmeliydi.");
        
        var responseBody =
            await response
                .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<ReservationListQueryResult>>>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi.");

        responseBody.Data.Should().HaveCount(reservations.Count,
            "Response body içersindeki rezervasyonların sayısı, oluşturulan tüm reservasyonların sayısı ile aynı olmalıydı.");
    }
    
    // SUCCESS TEST END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task GetPanelReservationList_WhenUserNotLoggedIn_ShouldReturn401()
    {
        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı giriş yapmadığı için durum kodu UNAUTHORIZED (401) olarak dönmeliydi.");
    }
    
    [Fact]
    public async Task GetPanelReservationList_WhenUserHasNotPermissionForThisRoute_ShouldReturn403()
    {
        var adminSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminSetup.AccessToken);

        var response = await Client.GetAsync(Route);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının bu işlem için yetkisi olmadığı için, 403 FORBIDDEN durum kodu dönmeliydi.");
    }
    
    // ERROR TESTS END
    
    // SETUP
    private async Task<IReadOnlyList<Reservation>> SeedReservationAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var user1 = await CreateVanillaUserAsync();
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
            Email = user1.User.Email!,
            UserId = user1.User.Id,
            FullName = user1.User.GetFullName(),
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