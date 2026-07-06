using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class RefreshTokenExample : IExamplesProvider<LoginControllerResponse>
{
    public LoginControllerResponse GetExamples()
    {
        return new LoginControllerResponse
        {
            Message = "Nullable message",
            Code = Codes.TOKEN_REFRESHED,
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuYXV0aDAuY29tLyIsImF1ZCI6Imh0dHBzOi8vYXBpLmV4YW1wbGUuY29tLyIsInN1YiI6InVzcl8xMjMiLCJleHAiOjE0NTg4NzIxOTZ9.CA7eaHjIHz5NxeIJoFK9krqaeZrPLwmMmgI_XiQiIkQ",
        };
    }
}