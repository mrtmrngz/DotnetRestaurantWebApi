using RestaurantApi.Application.Common.Enums;
using RestaurantApi.Application.Models.Responses.SuccessResponse;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.SuccessExamples;

public class LoginExample : IExamplesProvider<LoginResponse>
{
    public LoginResponse GetExamples()
    {
        return new LoginResponse()
        {
            Code = Codes.LOGIN_SUCCESS,
            Message = "Giriş işlemi başarılı.",
            AccessToken =
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2V4YW1wbGUuYXV0aDAuY29tLyIsImF1ZCI6Imh0dHBzOi8vYXBpLmV4YW1wbGUuY29tLyIsInN1YiI6InVzcl8xMjMiLCJleHAiOjE0NTg4NzIxOTZ9.CA7eaHjIHz5NxeIJoFK9krqaeZrPLwmMmgI_XiQiIkQ"

        };
    }
}