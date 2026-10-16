using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminRestaurantTableListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>> GetExamples()
    {
        var tables = new List<GetRestaurantTableListQueryResult>
        {
            new GetRestaurantTableListQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-1111-4a2b-8c3d-000000000001"),
                Number = 1,
                GuestCount = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new GetRestaurantTableListQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-2222-4a2b-8c3d-000000000002"),
                Number = 10,
                GuestCount = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new GetRestaurantTableListQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-3333-4a2b-8c3d-000000000003"),
                Number = 15,
                GuestCount = 6,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRestaurantTableListQueryResult>>(data: tables);
    }
}
