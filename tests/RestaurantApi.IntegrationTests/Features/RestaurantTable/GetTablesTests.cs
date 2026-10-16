using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApi.Application.Common;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.IntegrationTests.Extension;
using RestaurantApi.IntegrationTests.Setup;
using RestaurantApi.Persistence.Context;

namespace RestaurantApi.IntegrationTests.Features.RestaurantTable;

public class GetTablesTests: BaseIntegrationTest
{
    public GetTablesTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    // SUCCESS TESTS START

    [Fact]
    public async Task GetTables_WhenCacheIsEmpty_ShouldReturnTablesFromDatabase()
    {
        await SeedTablesAsync();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/tables");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa listesi başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().HaveCount(3, "Veritabanındaki tüm masalar dönmeliydi.");

        body.Data.Select(t => t.Number).Should().BeEquivalentTo(new[] { 1, 10, 15 },
            "Masalar veritabanından gelmeliydi.");
        body.Data.Should().BeInAscendingOrder(t => t.Number, "Masalar numarasına göre sıralı dönmeliydi.");

        body.Data.Single(t => t.Number == 1).GuestCount.Should().Be(4,
            "Entity'deki Capacity değeri GuestCount olarak maplenmeliydi.");
        body.Data.Single(t => t.Number == 10).GuestCount.Should().Be(2);
        body.Data.Single(t => t.Number == 1).IsActive.Should().BeTrue();
        body.Data.Single(t => t.Number == 15).IsActive.Should().BeFalse();

        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var cachedTables = await cache.GetAsync<List<GetRestaurantTableListQueryResult>>(
            CacheKeys.AdminRestaurantTables());

        cachedTables.Should().NotBeNull(
            "Cache boş olduğu için masalar veritabanından okunup redise yazılmalıydı.");
        cachedTables!.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTables_WhenCacheHasData_ShouldReturnTablesFromRedis()
    {
        var databaseTable = await SeedTableAsync(number: 1, capacity: 4);
        await SeedCacheAsync(new List<GetRestaurantTableListQueryResult>
        {
            new GetRestaurantTableListQueryResult
            {
                Id = Guid.NewGuid(),
                Number = 77,
                GuestCount = 9,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        });

        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/tables");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa listesi başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().HaveCount(1, "Cache dolu olduğu için yalnızca cached veri dönmeliydi.");
        body.Data[0].Number.Should().Be(77, "Masalar redisten gelmeliydi.");
        body.Data[0].GuestCount.Should().Be(9);
        body.Data.Should().NotContain(t => t.Number == databaseTable.Number,
            "Cache dolu olduğu için veritabanına gidilmemeliydi.");
    }

    [Fact]
    public async Task GetTables_WhenAllFiltersProvided_ShouldReturnOnlyMatchingTables()
    {
        await SeedFilterTablesAsync();
        var setupResult = await CreateAdminUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/tables?is-active=false&min-guest=5&max-guest=9&number=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Masa listesi başarılı bir şekilde dönmeliydi.");

        var body = await response
            .ReadContentAsAsync<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>();

        body.Should().NotBeNull("Response body null olmamalıydı.");
        body!.Data.Should().HaveCount(2,
            "Filtrelere uyan yalnızca 5 ve 15 numaralı masalar dönmeliydi.");
        body.Data.Select(t => t.Number).Should().BeEquivalentTo(new[] { 5, 15 },
            "Pasif, 5-9 misafir kapasiteli ve numarasında '5' geçen masalar dönmeliydi.");
    }

    // SUCCESS TESTS END

    // ERROR TESTS START

    [Fact]
    public async Task GetTables_WhenUserUnauthorized_ShouldReturn401()
    {
        var response = await Client.GetAsync("/api/tables");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Kullanıcı login olmadığı için 401 UNAUTHORIZED dönmeliydi.");
    }

    [Fact]
    public async Task GetTables_WhenUserNotAdmin_ShouldReturn403()
    {
        var setupResult = await CreateVanillaUserAsync();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", setupResult.AccessToken);

        var response = await Client.GetAsync("/api/tables");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Kullanıcı admin olmadığı için 403 FORBIDDEN dönmeliydi.");
    }

    // ERROR TESTS END

    // SETUP

    private async Task SeedTablesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.RestaurantTables.AddRange(
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 1, Capacity = 4, IsActive = true
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 10, Capacity = 2, IsActive = true
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 15, Capacity = 6, IsActive = false
            });

        await context.SaveChangesAsync();
    }

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

    private async Task SeedCacheAsync(IReadOnlyList<GetRestaurantTableListQueryResult> tables)
    {
        using var scope = Factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        await cache.SetAsync(CacheKeys.AdminRestaurantTables(), tables, TimeSpan.FromMinutes(30));
    }

    private async Task SeedFilterTablesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

        context.RestaurantTables.AddRange(
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 5, Capacity = 6, IsActive = false
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 15, Capacity = 6, IsActive = false
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 6, Capacity = 6, IsActive = false
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 25, Capacity = 6, IsActive = true
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 50, Capacity = 3, IsActive = false
            },
            new Domain.Entities.RestaurantTable
            {
                Id = Guid.NewGuid(), Number = 55, Capacity = 12, IsActive = false
            });

        await context.SaveChangesAsync();
    }
}
