using RestaurantApi.Application.Features.RestaurantTable.Queries.GetAvailableTablesQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AvailableRestaurantTableListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>> GetExamples()
    {
        var tables = new List<GetAvailableTablesQueryResult>
        {
            new GetAvailableTablesQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-1111-4a2b-8c3d-000000000001"),
                Number = 1,
                Capacity = 4
            },
            new GetAvailableTablesQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-2222-4a2b-8c3d-000000000002"),
                Number = 10,
                Capacity = 6
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetAvailableTablesQueryResult>>(data: tables);
    }
}
