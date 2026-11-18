using RestaurantApi.Application.Features.Users.Queries.GetUserRolesQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserRolesResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>> GetExamples()
    {
        var mockData = new List<GetUserRolesQueryResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = AppRoles.Admin,
                HasRole = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = AppRoles.User,
                HasRole = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "KITCHEN",
                HasRole = false
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserRolesQueryResult>>(data: mockData);
    }
}
