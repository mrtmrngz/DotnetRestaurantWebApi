using RestaurantApi.Application.Features.Auth.Commands.Login;
using Swashbuckle.AspNetCore.Filters;

namespace RestaurantApi.WebApi.Swagger.Examples.RequestExamples.AuthExamples;

public class LoginRequestExample: IExamplesProvider<LoginCommand>
{
    public LoginCommand GetExamples()
    {
        return new LoginCommand(
            Email: "john@gmail.com",
            Password:"StrongPassword123"
        );
    }
}