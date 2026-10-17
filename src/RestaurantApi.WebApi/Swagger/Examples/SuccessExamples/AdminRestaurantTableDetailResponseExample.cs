using RestaurantApi.Application.Features.RestaurantTable.Queries.GetRestaurantTableDetailQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class AdminRestaurantTableDetailResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>>
{
    public GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult> GetExamples()
    {
        return new GeneralSuccessResponseWithData<GetRestaurantTableDetailQueryResult>(
            data: new GetRestaurantTableDetailQueryResult
            {
                Id = Guid.Parse("1a2b3c4d-1111-4a2b-8c3d-000000000001"),
                Number = 1,
                GuestCount = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        );
    }
}
