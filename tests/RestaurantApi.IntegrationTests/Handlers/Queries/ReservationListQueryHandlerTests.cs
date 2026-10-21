using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Reservation.Queries.ReservationListQuery;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Handlers.Queries;

public class ReservationListQueryHandlerTests: BaseIntegrationTest
{
    public ReservationListQueryHandlerTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }
    
    // SUCCESS TESTS START

    [Fact]
    public async Task ReservationListQueryHandler_WhenRequestIsCurrentUser_ShouldReturnJustCurrentUserReservations()
    {
        var (reservations, currentUser) = await SeedReservationAsync();
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new ReservationListQuery(UserId: currentUser.Id,
            RequestType: ReservationListRequestEnums.CurrentUser);

        var response = await mediator.Send(query);

        response.Should().NotBeNull("Response null gelmemeliydi.");
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS,
            "Response içerisindeki code FETH_DATA_SUCCESS gelmeliydi.");

        response.Data.Should().HaveCount(1,
            "Response içerisinde, kullanıcının kendisinin olan 1 adet reservasyon olmalıydı.");

        response.Data[0].Id.Should().Be(reservations[0].Id,
            "Response içerisindeki rezervasyonun id'si ile kullanıcının rezervasyonun id'si eşleşmeliydi.");
    }
    
    [Fact]
    public async Task ReservationListQueryHandler_WhenRequestIsCurrentUserButUserHasNotAnyReservation_ShouldReturnEmptyArray()
    {
        var currentUser = await CreateVanillaUserAsync();
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new ReservationListQuery(UserId: currentUser.User.Id,
            RequestType: ReservationListRequestEnums.CurrentUser);

        var response = await mediator.Send(query);

        response.Should().NotBeNull("Response null gelmemeliydi.");
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS,
            "Response içerisindeki code FETH_DATA_SUCCESS gelmeliydi.");

        response.Data.Should().HaveCount(0,
            "Kullanıcının rezervasyonu olmadığı için response içerisindeki data boş array gelmeliydi.");
    }
    
    [Fact]
    public async Task ReservationListQueryHandler_WhenRequestIsPanel_ShouldReturnAllReservations()
    {
        var (reservations, _) = await SeedReservationAsync();
        var adminUserSetup = await CreateAdminUserAsync();
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new ReservationListQuery(UserId: null, RequestType: ReservationListRequestEnums.PanelUser);

        var response = await mediator.Send(query);

        response.Should().NotBeNull("Response null gelmemeliydi.");
        response.Code.Should().Be(Codes.FETH_DATA_SUCCESS,
            "Response içerisindeki code FETH_DATA_SUCCESS gelmeliydi.");

        response.Data.Should().HaveCount(reservations.Count,
            "İsteği yapan panel kullanıcısı olduğu için response data tüm rezervasyonları döndürmeliydi..");
    }
    
    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task ReservationListQueryHandler_WhenValidationErrorsAcquired_ShouldThrowValidationExcepiton()
    {
        var (reservations, _) = await SeedReservationAsync();
        var adminUserSetup = await CreateAdminUserAsync();
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new ReservationListQuery(UserId: adminUserSetup.User.Id, RequestType: ReservationListRequestEnums.PanelUser);

        Func<Task> act = async () => await mediator.Send(query);

        await act.Should().ThrowAsync<ValidationException>("Validasyon hatası fırlatmalıydı.");
    }
    
    // ERROR TESTS END
    
    
    // SETUP
    private async Task<(IReadOnlyList<Reservation> Reservations, AppUser CurrentUser)> SeedReservationAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var user1 = await CreateVanillaUserAsync();
        var user2 = await CreateVanillaUserAsync();

        RestaurantTable table = new RestaurantTable
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

        return (Reservations: reservationList, CurrentUser: user1.User);
    }
}