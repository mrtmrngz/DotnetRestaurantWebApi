using RestaurantApi.Application.Features.Roles.Queries.GetRoleInfoQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetRoleInfoResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetRoleInfoQueryResult>>
{
    public GeneralSuccessResponseWithData<GetRoleInfoQueryResult> GetExamples()
    {
        var mockData = new GetRoleInfoQueryResult
        {
            Id = Guid.NewGuid(),
            Name = "KITCHEN",
            NormalizedName = "KITCHEN",
            IsSystemRole = false
        };

        return new GeneralSuccessResponseWithData<GetRoleInfoQueryResult>(data: mockData);
    }
}
