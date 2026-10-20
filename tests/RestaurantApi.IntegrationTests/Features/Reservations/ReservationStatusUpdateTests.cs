using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.Reservation.Commands.ReservationStatusUpdateCommand;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Entities;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.Reservations;

public class ReservationStatusUpdateTests : BaseIntegrationTest
{
    private const string Route = "/api/reservations/status-update/";

    public ReservationStatusUpdateTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task ReservationStatusUpdate_WhenValidData_ShouldReturn200()
    {
        var userSetup = await CreateAdminUserAsync();
        var seedResult = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            r => r.Status = ReservationStatus.Pending);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{seedResult.Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Rezervasyon durumu başarılı bir şekilde güncellenmeliydi.");

        var responseBody = await response.ReadContentAsAsync<BaseResponse>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi");
        responseBody.Code.Should().Be(Codes.CONTENT_UPDATED_SUCCESS,
            "Response body içerisindeki kod CONTENT_UPDATED_SUCCESS olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var reservation = await context.Reservations.SingleAsync();

            reservation.Should().NotBeNull("Rezervasyon null olmamalıydı.");

            reservation.Status.Should().Be(ReservationStatus.Confirmed,
                "Rezervasyon durumu güncellenmek istenen değer ile aynı olmalıydı.");
        }
    }
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenRequestedStatusSameAsTargetReservation_ShouldReturn200()
    {
        var userSetup = await CreateAdminUserAsync();
        var seedResult = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(1),
            r => r.Status = ReservationStatus.Confirmed);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{seedResult.Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Rezervasyon durumu başarılı bir şekilde güncellenmeliydi.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var reservation = await context.Reservations.SingleAsync();

            reservation.Should().NotBeNull("Rezervasyon null olmamalıydı.");

            reservation.Status.Should().Be(ReservationStatus.Confirmed,
                "Rezervasyon durumu güncellenmek istenen değer ile aynı olmalıydı.");
        }
    }

    // SUCCESS TESTS END
    
    // ERROR TESTS START
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenValidationErrorsAcquired_ShouldReturn400()
    {
        var userSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);
        
        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: "invalid status");

        var response = await Client.PatchAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Validasyon hataları meydana geldiği için 400 BAD REQUEST hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenUserNotLoggedIn_ShouldReturn401()
    {
        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcının giriş yapmadığı için 401 NOT_FOUND hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenUserHasNotPermissionForThisRoute_ShouldReturn403()
    {
        var userSetup = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcının bu işlem için yetkisi olmadığı için 403 FORBIDDEN hata kodu dönmeliydi.");
    }
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenReservationNotExist_ShouldReturn404()
    {
        var userSetup = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{Guid.NewGuid()}", command);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Rezervasyon bulunamadığı için 404 NOT FOUND hata kodu dönmeliydi.");

        var responseBody = await response.ReadContentAsAsync<NotFoundException>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi");
        responseBody.Code.Should().Be(Codes.NOT_FOUND,
            "Response body içerisindeki kod NOT_FOUND olmalıydı.");
    }
    
    [Fact]
    public async Task ReservationStatusUpdate_WhenReservationEndTimePassing_ShouldReturn422()
    {
        var userSetup = await CreateAdminUserAsync();
        var seedResult = await SeedReservationAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1),
            r => r.Status = ReservationStatus.Pending);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userSetup.AccessToken);

        ReservationStatusUpdateCommand
            command = new ReservationStatusUpdateCommand(Status: ReservationStatus.Confirmed);

        var response = await Client.PatchAsJsonAsync($"{Route}{seedResult.Id}", command);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "Rezervasyon bitiş tarihi geçtiği için durum güncellenmemeliydi.");

        var responseBody = await response.ReadContentAsAsync<UnprocessableEntityError>();

        responseBody.Should().NotBeNull("Response body null gelmemeliydi");
        responseBody.Code.Should().Be(Codes.UNPROCESSABLE_ENTITY,
            "Response body içerisindeki kod UNPROCESSABLE_ENTITY olmalıydı.");

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

            var reservation = await context.Reservations.SingleAsync();

            reservation.Should().NotBeNull("Rezervasyon null olmamalıydı.");

            reservation.Status.Should().Be(ReservationStatus.Pending,
                "Rezervasyonun bitiş tarihi değişmediği için durum değişmemeliydi.");
        }
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