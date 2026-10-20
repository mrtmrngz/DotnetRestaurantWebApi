using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Reservation.Commands.CancelReservationCommand;
using RestaurantApi.Domain.Entities;
using RestaurantApi.Domain.Identity;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;
using RestaurantApi.WebApi.Swagger.Examples.ErrorExamples;

namespace RestaurantApi.IntegrationTests.Handlers.Commands;

public class CancelReservationCommandHandlerTests : BaseIntegrationTest
{
    public CancelReservationCommandHandlerTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }


    // SUCCESS TESTS START

    [Fact]
    public async Task
        CancelReservationCommandHandler_WhenCurrentUserReservationValidForCancellation_ShouldReturnBaseResponse()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            CancelReservationTypes.CurrentUser, userSetup.User);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: userSetup.User.Id,
            CancelReservationTypes.CurrentUser);

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeliydi.");
        response.Code.Should().Be(Codes.RESERVATION_CANCELLED_SUCCESS,
            "Response içerisindeki code RESERVATION_CANCELLED_SUCCESS olarak dönmeliydi.");

        var currentReservation = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id && x.UserId == userSetup.User.Id);
        currentReservation.Should().NotBeNull("Rezervasyon null gelmemeliydi.");

        currentReservation.Status.Should().Be(ReservationStatus.Canceled, "Rezervasyon iptal edilmeliydi.");
    }
    
    [Fact]
    public async Task
        CancelReservationCommandHandler_WhenPanelUserValidForCancellationReservation_ShouldReturnBaseResponse()
    {
        var userSetup = await CreateAdminUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            CancelReservationTypes.Admin, user: null);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: null,
            CancelReservationTypes.Admin);

        var response = await mediator.Send(command);

        response.Should().NotBeNull("Response null gelmemeliydi.");
        response.Code.Should().Be(Codes.RESERVATION_CANCELLED_SUCCESS,
            "Response içerisindeki code RESERVATION_CANCELLED_SUCCESS olarak dönmeliydi.");

        var currentReservation = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
        currentReservation.Should().NotBeNull("Rezervasyon null gelmemeliydi.");

        currentReservation.Status.Should().Be(ReservationStatus.Canceled, "Rezervasyon iptal edilmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CancelReservationCommandHandler_WhenCurrentUserReservationNotFound_ShouldThrowNotFoundException()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            CancelReservationTypes.CurrentUser, userSetup.User);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: Guid.NewGuid(),
            CancelReservationTypes.CurrentUser);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should()
            .ThrowAsync<NotFoundException>(
                "Kullanının rezervasyonu kendisine ait olmadığı için hata 404 hatası fırlatmalıydı.");
        
        var currentReservation = await context.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
        currentReservation.Should().NotBeNull("Rezervasyon null gelmemeliydi.");

        currentReservation.Status.Should().NotBe(ReservationStatus.Canceled, "Rezervasyon iptal edilmemeliydi.");
    }

    [Fact]
    public async Task
        CancelReservationCommandHandler_WhenReservationAlreadyCanceledAndRequestTypeIsNotAdmin_ShouldThrowConflictException()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            CancelReservationTypes.CurrentUser, userSetup.User, r => r.Status = ReservationStatus.Canceled);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: res.UserId,
            CancelReservationTypes.CurrentUser);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should()
            .ThrowAsync<ConflictException>(
                "Rezervasyon zaten iptal edildiği için hata göndermeliydi.");
    }
    
    [Fact]
    public async Task
        CancelReservationCommandHandler_WhenReservationStatusCompletedAndRequestTypeIsNotAdmin_ShouldThrowUnprocessableEntityException()
    {
        var userSetup = await CreateVanillaUserAsync();
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            CancelReservationTypes.CurrentUser, userSetup.User, r => r.Status = ReservationStatus.Completed);
        
        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: res.UserId,
            CancelReservationTypes.CurrentUser);
        
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should()
            .ThrowAsync<UnprocessableEntityError>(
                "Rezervasyon status tamamlandığı için hata göndermeliydi.");
    }
    
    [Fact]
    public async Task
        CancelReservationCommandHandler_WhenReservationEndTimePassing_ShouldThrowUnprocessableEntityError()
    {
        var res = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-2),
            CancelReservationTypes.Admin, user: null);

        await using var scope = Factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var command = new CancelReservationCommand(ReservationId: res.Id, UserId: null,
            CancelReservationTypes.Admin);

        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().ThrowAsync<UnprocessableEntityError>("Rezervasyon bitiş tarihi geçtiği için hata fırlatmalıydı.");
    }

    // ERROR TESTS END


    // SETUP
    private async Task<Domain.Entities.Reservation> SeedReservationAsync(DateTime startTime, DateTime endTime,
        CancelReservationTypes type, AppUser? user, Action<Reservation>? customConfig = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

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

        Reservation reservation = new Reservation
        {
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            StartTime = startTime,
            EndTime = endTime,
            IsDeleted = false,
            RestaurantTableId = table.Id,
            Note = ""
        };

        if (type == CancelReservationTypes.CurrentUser && user is not null)
        {
            reservation.UserId = user.Id;
            reservation.FullName = user.GetFullName();
            reservation.Email = user.Email!;
            reservation.PhoneNumber = user.PhoneNumber ?? "+9005441234567";
            reservation.Status = ReservationStatus.Pending;
        }
        else
        {
            reservation.FullName = "Jane Doe";
            reservation.Email = "jane@mail.com";
            reservation.PhoneNumber = "+9005441234567";
            reservation.Status = ReservationStatus.Confirmed;
        }

        customConfig?.Invoke(reservation);

        context.Reservations.Add(reservation);

        await context.SaveChangesAsync();

        return reservation;
    }
}