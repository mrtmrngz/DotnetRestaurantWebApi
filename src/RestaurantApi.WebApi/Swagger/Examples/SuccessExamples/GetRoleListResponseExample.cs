using RestaurantApi.Application.Features.Roles.Queries.GetRoleListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetRoleListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>> GetExamples()
    {
        var mockData = new List<GetRoleListQueryResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "ADMIN",
                UserCount = 2,
                PermissionCount = 38,
                IsSystemRole = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "USER",
                UserCount = 156,
                PermissionCount = 0,
                IsSystemRole = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "KITCHEN",
                UserCount = 7,
                PermissionCount = 5,
                IsSystemRole = false
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetRoleListQueryResult>>(data: mockData);
    }
}
