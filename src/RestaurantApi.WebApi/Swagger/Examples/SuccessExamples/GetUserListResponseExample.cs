using RestaurantApi.Application.Features.Users.Queries.GetUserListQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserListResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>>
{
    public GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>> GetExamples()
    {
        var mockData = new List<GetUserListQueryResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FullName = "Admin User",
                Email = "admin@example.com",
                PhoneNumber = "+905551112233",
                EmailConfirmed = true,
                TwoFactorStatus = true,
                Roles = new List<string> { AppRoles.Admin }
            },
            new()
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john.doe@example.com",
                PhoneNumber = null,
                EmailConfirmed = false,
                TwoFactorStatus = false,
                Roles = new List<string> { AppRoles.User }
            }
        };

        return new GeneralSuccessResponseWithData<IReadOnlyList<GetUserListQueryResult>>(data: mockData);
    }
}
