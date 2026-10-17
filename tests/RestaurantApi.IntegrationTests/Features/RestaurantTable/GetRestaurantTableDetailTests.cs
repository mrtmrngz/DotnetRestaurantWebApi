using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Common.Exceptions;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;
using RestaurantApi.Application.Models.Responses.ErrorResponses;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class GetRestaurantTableDetailTests: BaseIntegrationTest
{
    public GetRestaurantTableDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetRestaurantTableDetail_WhenTableExists_ShouldReturnTableDetail()
    {
        var table = await SeedTableAsync(number: 7, capacity: 5, isActive: false);
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa detayı başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Id.Should().Be(table.Id);
        body.Data.Number.Should().Be(7, "Masa numarası gönderilen masa ile aynı olmalıydı.");
        body.Data.GuestCount.Should().Be(5, "Entity'deki Capacity değeri GuestCount olarak maplenmeliydi.");
        body.Data.IsActive.Should().BeFalse("Pasif masanın detayı da dönmeliydi.");
        body.Data.CreatedAt.Should().BeCloseTo(table.CreatedAt, TimeSpan.FromSeconds(1),
            "Masa oluşturulma tarihi dönmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetRestaurantTableDetail_WhenTableIdIsEmpty_ShouldReturn400()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/tables/{Guid.Empty}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Masa id boş olduğu için 400 BAD REQUEST dönmeliydi.");

        var body = await response.ReadContentAsAsync<ValidationErrorResponse>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.VALIDATION_ERROR, "VALIDATION_ERROR kodu dönmeliydi.");
        body.Errors.Should().Contain(e => e.Message == "Masa id boş olamaz.",
            "Masa id boş olduğu için ilgili validasyon hatası dönmeliydi.");
    }

    [Fact]
    public async Task GetRestaurantTableDetail_WhenTableNotExist_ShouldReturn404()
    {
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/tables/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "Masa bulunamadığı için 404 NOT FOUND dönmeliydi.");

        var body = await response.ReadContentAsAsync<NotFoundException>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Code.Should().Be(Codes.NOT_FOUND, "NOT_FOUND kodu dönmeliydi.");
    }

    [Fact]
    public async Task GetRestaurantTableDetail_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync($"/api/tables/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task GetRestaurantTableDetail_WhenUserNotAdmin_ShouldReturn403()
    {
        var table = await SeedTableAsync(number: 7, capacity: 5);
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync($"/api/tables/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task<Domain.Entities.RestaurantTable> SeedTableAsync(int number, int capacity, bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        var table = new Domain.Entities.RestaurantTable
        {
            Id = Guid.NewGuid(),
            Number = number,
            Capacity = capacity,
            IsActive = isActive
        };

        context.RestaurantTables.Add(table);

        await context.SaveChangesAsync();

        return table;
    }
}
