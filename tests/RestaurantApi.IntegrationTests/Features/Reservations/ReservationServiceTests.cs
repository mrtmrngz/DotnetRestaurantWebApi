using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class ReservationServiceTests: BaseIntegrationTest
{
    private const bool IsPanelRequest = false;

    public ReservationServiceTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenEverythingIsValid_ShouldNotThrowAnything()
    {
        var table = await SeedTableAsync(capacity: 6, isActive: true);

        var nowUtc = DateTime.UtcNow;

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(table.Id, guestCount: 4,
            startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: IsPanelRequest, ctx: CancellationToken.None);

        await act.Should()
            .NotThrowAsync("Masa aktif, kapasitesi yeterli ve istenen aralık boş olduğu için hata fırlatılmamalıydı.");

        await uow.CommitTransactionAsync();
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenTableNotExist_ShouldThrowNotFoundException()
    {
        var nowUtc = DateTime.UtcNow;
        var notExistingTableId = Guid.NewGuid();

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(notExistingTableId,
            guestCount: 4, startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: IsPanelRequest, ctx: CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.");

        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenGuestCountIsAboveTableCapacity_ShouldThrowUnprocessableEntityError()
    {
        var table = await SeedTableAsync(capacity: 4, isActive: true);

        var nowUtc = DateTime.UtcNow;

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(table.Id, guestCount: 6,
            startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: IsPanelRequest, ctx: CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityError>()
            .WithMessage("*kapasitesi yetersiz*");

        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenRequestedTimeRangeIsOccupied_ShouldThrowConflictException()
    {
        var table = await SeedTableAsync(capacity: 6, isActive: true);

        var nowUtc = DateTime.UtcNow;

        await SeedReservationAsync(table.Id, nowUtc.AddHours(1), nowUtc.AddHours(4));

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(table.Id, guestCount: 4,
            startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: IsPanelRequest, ctx: CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Seçilen masa, istenen zaman aralığında dolu.");

        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenTableIsPassiveAndRequestIsFromPanel_ShouldThrowConflictException()
    {
        var table = await SeedTableAsync(capacity: 6, isActive: false);

        var nowUtc = DateTime.UtcNow;

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(table.Id, guestCount: 4,
            startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: true, ctx: CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Pasif masa için rezervasyon oluşturulamaz.",
                "panel akışında masanın pasif olduğu açıkça belirtilebilir.");

        await uow.RollbackTransactionAsync();
    }

    [Fact]
    public async Task ShouldTableBeReservableAsync_WhenTableIsPassiveAndRequestIsFromCurrentUser_ShouldThrowNotFoundException()
    {
        var table = await SeedTableAsync(capacity: 6, isActive: false);

        var nowUtc = DateTime.UtcNow;

        await using var scope = CreateScope(out var reservationService, out var uow);

        await uow.BeginTransaction();

        Func<Task> act = async () => await reservationService.ShouldTableBeReservableAsync(table.Id, guestCount: 4,
            startTimeUtc: nowUtc.AddHours(2), endTimeUtc: nowUtc.AddHours(3), nowUtc: nowUtc,
            isPanelRequest: false, ctx: CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Masa bulunamadı.",
                "current user akışında masanın pasif olduğu bilgisi sızdırılmamalıdır.");

        await uow.RollbackTransactionAsync();
    }

    // ERROR TESTS END

    // SETUP

    private AsyncServiceScope CreateScope(out IReservationService reservationService, out IUnitOfWork uow)
    {
        var scope = Factory.Services.CreateAsyncScope();

        reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
        uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        return scope;
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

    private async Task SeedReservationAsync(Guid tableId, DateTime startTimeUtc, DateTime endTimeUtc,
        string status = ReservationStatus.Pending)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.Reservations.Add(new Domain.Entities.Reservation
        {
            Id = Guid.NewGuid(),
            RestaurantTableId = tableId,
            GuestCount = 2,
            Status = status,
            FullName = "Test User",
            PhoneNumber = "5551112233",
            Email = "test@test.com",
            Note = "test",
            StartTime = startTimeUtc,
            EndTime = endTimeUtc,
            IsDeleted = false
        });

        await context.SaveChangesAsync();
    }
}
