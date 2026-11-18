using RestaurantApi.Application.Features.Users.Queries.GetUserInfoQuery;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using RestaurantApi.Domain.Constants;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class GetUserInfoResponseExample: IExamplesProvider<GeneralSuccessResponseWithData<GetUserInfoQueryResult>>
{
    public GeneralSuccessResponseWithData<GetUserInfoQueryResult> GetExamples()
    {
        var mockData = new GetUserInfoQueryResult
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Surname = "User",
            Email = "admin@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+905551112233",
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = false,
            Roles = new List<string> { AppRoles.Admin }
        };

        return new GeneralSuccessResponseWithData<GetUserInfoQueryResult>(data: mockData);
    }
}
