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
using RestaurantApi.Application.Features.RestaurantTable.Commands.CreateRestaurantTableCommand;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class CreateTableTests: BaseIntegrationTest
{
    public CreateTableTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task CreateTable_WhenRequestIsValid_ShouldReturn201AndClearCache()
    {
        await SeedCachesAsync();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new CreateRestaurantTableCommand(Number: 5, Capacity: 4);

        var response = await Client.PostAsJsonAsync("/api/tables", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created, "Masa başarılı bir şekilde oluşturulmalıydı.");

        var body = await response.ReadContentAsAsync<BaseResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONTENT_CREATED_SUCCESS,
            "Oluşturma işlemi sonucunda CONTENT_CREATED_SUCCESS kodu dönmeliydi.");

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.Number == 5);

        table.Should().NotBeNull("Masa veritabanına kaydedilmeliydi.");
        table!.Capacity.Should().Be(4, "Masa kapasitesi gönderilen değer ile aynı olmalıydı.");
        table.IsActive.Should().BeTrue("Yeni oluşturulan masa aktif olmalıydı.");

        (await cache.GetAsync<string>(CacheKeys.AdminRestaurantTables())).Should().BeNull(
            "Masa oluşturulduğu için admin masa listesi redisten silinmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task CreateTable_WhenTableNumberIsInvalid_ShouldReturn400()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new CreateRestaurantTableCommand(Number: 0, Capacity: 4);

        var response = await Client.PostAsJsonAsync("/api/tables", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Masa numarası 0 olduğu için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
    }

    [Fact]
    public async Task CreateTable_WhenTableNumberAlreadyExists_ShouldReturn409()
    {
        await SeedTableAsync(number: 5);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new CreateRestaurantTableCommand(Number: 5, Capacity: 4);

        var response = await Client.PostAsJsonAsync("/api/tables", command);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Masa numarası zaten kullanıldığı için 409 CONFLICT dönmeliydi.");

        var body = await response.ReadContentAsAsync<ConflictException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.CONFLICT, "CONFLICT kodu dönmeliydi.");
    }

    [Fact]
    public async Task CreateTable_WhenUserUnauthorized_ShouldReturn401()
    {
        var command = new CreateRestaurantTableCommand(Number: 5, Capacity: 4);

        var response = await Client.PostAsJsonAsync("/api/tables", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task CreateTable_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var command = new CreateRestaurantTableCommand(Number: 5, Capacity: 4);

        var response = await Client.PostAsJsonAsync("/api/tables", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task SeedCachesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.AdminRestaurantTables(), "tables", TimeSpan.FromHours(1));
    }

    private async Task SeedTableAsync(int number, int capacity = 4)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.RestaurantTables.Add(new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = number,
            Capacity = capacity,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }
}
